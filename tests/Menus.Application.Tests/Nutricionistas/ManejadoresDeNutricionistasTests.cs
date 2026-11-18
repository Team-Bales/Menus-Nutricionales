using Menus.Application.Comun;
using Menus.Application.Nutricionistas.ActualizarNutricionista;
using Menus.Application.Nutricionistas.AsignarRolJefe;
using Menus.Application.Nutricionistas.AutorizarDieta;
using Menus.Application.Nutricionistas.ListarNutricionistas;
using Menus.Application.Nutricionistas.ObtenerNutricionista;
using Menus.Application.Nutricionistas.RevocarDieta;
using Menus.Application.Tests.Catalogo.Dietas;
using Menus.Application.Tests.Catalogo.Nomencladores;
using Menus.Domain;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Nutricionistas;

/// <summary>Pruebas de los manejadores de nutricionistas del #71 contra los datos en memoria.</summary>
public sealed class ManejadoresDeNutricionistasTests
{
    private readonly BancoDeNutricionistasFalso _banco = new();
    private readonly BancoDeDietasFalso _dietas = new();
    private readonly BancoDeNomencladoresFalso _nomencladores = new();

    public ManejadoresDeNutricionistasTests()
    {
        _nomencladores.Especialidades.Add(new Especialidad(BancoDeNutricionistasFalso.EspecialidadClinica, "Clínica"));
        _nomencladores.Especialidades.Add(new Especialidad(BancoDeNutricionistasFalso.EspecialidadDeportiva, "Deportiva"));
    }

    // ---- Listar ----

    [Fact]
    public async Task Listar_devuelve_la_pagina_ordenada_por_nombre_con_el_total()
    {
        _banco.Agregar("Carla");
        _banco.Agregar("Ana");
        _banco.Agregar("Berta");

        var pagina = await Handler().HandleAsync(new ListarNutricionistasQuery(Page: 1, Size: 2), CancellationToken.None);

        Assert.Equal(3, pagina.Total);
        Assert.Equal(2, pagina.Data.Count);
        Assert.Equal(["Ana", "Berta"], pagina.Data.Select(n => n.Nombre));
    }

    [Fact]
    public async Task Listar_filtra_por_especialidad()
    {
        _banco.Agregar("Ana", BancoDeNutricionistasFalso.EspecialidadClinica);
        _banco.Agregar("Berta", BancoDeNutricionistasFalso.EspecialidadDeportiva);

        var pagina = await Handler().HandleAsync(
            new ListarNutricionistasQuery(BancoDeNutricionistasFalso.EspecialidadDeportiva), CancellationToken.None);

        var dto = Assert.Single(pagina.Data);
        Assert.Equal("Berta", dto.Nombre);
    }

    [Theory]
    [InlineData(0, 10, "Page")]
    [InlineData(1, 0, "Size")]
    [InlineData(1, 101, "Size")]
    public void El_validador_del_listado_rechaza_la_paginacion_invalida(int page, int size, string campo)
        => Assert.Equal(
            [campo],
            new ListarNutricionistasValidator().Validate(new ListarNutricionistasQuery(Page: page, Size: size))
                .Errors.Select(e => e.PropertyName));

    private ListarNutricionistasHandler Handler() => new(_banco.NutricionistasRepo);

    // ---- Obtener ----

    [Fact]
    public async Task Obtener_un_admin_ve_el_detalle_de_cualquier_nutricionista_con_sus_dietas()
    {
        var nutricionista = _banco.Agregar("Ana");
        var dieta = _dietas.Agregar("Dieta renal");
        nutricionista.AutorizarDieta(dieta.Id);

        var dto = await Obtener(RolesUsuario.Admin, Guid.NewGuid()).HandleAsync(
            new ObtenerNutricionistaQuery(nutricionista.Id), CancellationToken.None);

        Assert.Equal(nutricionista.Id, dto.Id);
        Assert.Equal("Ana", dto.Nombre);
        Assert.Equal([dieta.Id], dto.DietasAutorizadas.Select(d => d.DietaId));
    }

    [Fact]
    public async Task Obtener_un_nutricionista_puede_ver_su_propio_detalle()
    {
        var nutricionista = _banco.Agregar("Ana");

        var dto = await Obtener(RolesUsuario.Nutricionista, nutricionista.Id).HandleAsync(
            new ObtenerNutricionistaQuery(nutricionista.Id), CancellationToken.None);

        Assert.Equal(nutricionista.Id, dto.Id);
    }

    [Fact]
    public async Task Obtener_un_nutricionista_no_puede_ver_el_detalle_de_otro()
    {
        var nutricionista = _banco.Agregar("Ana");

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => Obtener(RolesUsuario.Nutricionista, Guid.NewGuid()).HandleAsync(
                new ObtenerNutricionistaQuery(nutricionista.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Obtener_un_paciente_no_puede_ver_el_detalle_de_un_nutricionista()
    {
        var nutricionista = _banco.Agregar("Ana");

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => Obtener(RolesUsuario.Paciente, Guid.NewGuid()).HandleAsync(
                new ObtenerNutricionistaQuery(nutricionista.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Obtener_sin_entidad_id_en_el_token_no_puede_ver_el_detalle()
    {
        var nutricionista = _banco.Agregar("Ana");

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => Obtener(RolesUsuario.Nutricionista, null).HandleAsync(
                new ObtenerNutricionistaQuery(nutricionista.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Obtener_un_nutricionista_que_no_existe_da_404()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Obtener(RolesUsuario.Admin, Guid.NewGuid()).HandleAsync(new ObtenerNutricionistaQuery(id), CancellationToken.None));

        Assert.Equal("nutricionista", error.Recurso);
        Assert.Equal(id, error.Id);
    }

    private ObtenerNutricionistaHandler Obtener(string rol, Guid? entidadId)
        => new(_banco.NutricionistasRepo, new IdentidadFalsa(rol, entidadId));

    // ---- Actualizar ----

    private ActualizarNutricionistaHandler Actualizar()
        => new(_banco.NutricionistasRepo, _nomencladores.EspecialidadesRepo, _banco.UnidadDeTrabajo);

    [Fact]
    public async Task Actualizar_cambia_el_nombre_y_la_especialidad()
    {
        var nutricionista = _banco.Agregar("Ana", BancoDeNutricionistasFalso.EspecialidadClinica);
        var nuevaEspecialidad = BancoDeNutricionistasFalso.EspecialidadDeportiva;

        await Actualizar().HandleAsync(
            new ActualizarNutricionistaCommand(nutricionista.Id, "  Ana Gómez  ", nuevaEspecialidad), CancellationToken.None);

        Assert.Equal("Ana Gómez", nutricionista.Nombre);
        Assert.Equal(nuevaEspecialidad, nutricionista.EspecialidadId);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_un_nutricionista_que_no_existe_da_404()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Actualizar().HandleAsync(
                new ActualizarNutricionistaCommand(id, "Ana", BancoDeNutricionistasFalso.EspecialidadClinica), CancellationToken.None));

        Assert.Equal("nutricionista", error.Recurso);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_con_una_especialidad_que_no_existe_da_404()
    {
        var nutricionista = _banco.Agregar("Ana");
        var especialidadInexistente = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Actualizar().HandleAsync(
                new ActualizarNutricionistaCommand(nutricionista.Id, "Ana", especialidadInexistente), CancellationToken.None));

        Assert.Equal("especialidad", error.Recurso);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Theory]
    [InlineData("", "Nombre")]
    [InlineData("   ", "Nombre")]
    public void El_validador_de_actualizar_rechaza_el_nombre_vacio(string nombre, string campo)
        => Assert.Equal(
            [campo],
            new ActualizarNutricionistaValidator().Validate(new ActualizarNutricionistaCommand(Guid.NewGuid(), nombre, Guid.NewGuid()))
                .Errors.Select(e => e.PropertyName));

    [Fact]
    public void El_validador_de_actualizar_rechaza_la_especialidad_vacia()
        => Assert.Equal(
            ["EspecialidadId"],
            new ActualizarNutricionistaValidator().Validate(new ActualizarNutricionistaCommand(Guid.NewGuid(), "Ana", Guid.Empty))
                .Errors.Select(e => e.PropertyName));

    // ---- AutorizarDieta ----

    [Fact]
    public async Task AutorizarDieta_agrega_la_autorizacion()
    {
        var nutricionista = _banco.Agregar("Ana");
        var dieta = _dietas.Agregar("Dieta renal");

        await Autorizar().HandleAsync(new AutorizarDietaCommand(nutricionista.Id, dieta.Id), CancellationToken.None);

        Assert.Single(nutricionista.DietasAutorizadas, a => a.DietaId == dieta.Id);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task AutorizarDieta_ya_autorizada_es_idempotente()
    {
        var nutricionista = _banco.Agregar("Ana");
        var dieta = _dietas.Agregar("Dieta renal");
        nutricionista.AutorizarDieta(dieta.Id);

        await Autorizar().HandleAsync(new AutorizarDietaCommand(nutricionista.Id, dieta.Id), CancellationToken.None);

        Assert.Single(nutricionista.DietasAutorizadas, a => a.DietaId == dieta.Id);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones); // ya estaba: no vuelve a escribir
    }

    [Fact]
    public async Task AutorizarDieta_un_nutricionista_que_no_existe_da_404()
    {
        var dieta = _dietas.Agregar("Dieta renal");
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Autorizar().HandleAsync(new AutorizarDietaCommand(id, dieta.Id), CancellationToken.None));

        Assert.Equal("nutricionista", error.Recurso);
    }

    [Fact]
    public async Task AutorizarDieta_una_dieta_que_no_existe_da_404()
    {
        var nutricionista = _banco.Agregar("Ana");
        var dietaId = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Autorizar().HandleAsync(new AutorizarDietaCommand(nutricionista.Id, dietaId), CancellationToken.None));

        Assert.Equal("dieta", error.Recurso);
        Assert.Empty(nutricionista.DietasAutorizadas);
    }

    [Fact]
    public void El_validador_de_autorizar_exige_la_dieta()
        => Assert.Equal(
            ["DietaId"],
            new AutorizarDietaValidator().Validate(new AutorizarDietaCommand(Guid.NewGuid(), Guid.Empty))
                .Errors.Select(e => e.PropertyName));

    private AutorizarDietaHandler Autorizar() => new(_banco.NutricionistasRepo, _dietas.DietasRepo, _banco.UnidadDeTrabajo);

    // ---- RevocarDieta ----

    [Fact]
    public async Task RevocarDieta_quita_la_autorizacion()
    {
        var nutricionista = _banco.Agregar("Ana");
        var dieta = _dietas.Agregar("Dieta renal");
        nutricionista.AutorizarDieta(dieta.Id);
        var handler = new RevocarDietaHandler(_banco.NutricionistasRepo, _banco.UnidadDeTrabajo);

        await handler.HandleAsync(new RevocarDietaCommand(nutricionista.Id, dieta.Id), CancellationToken.None);

        Assert.Empty(nutricionista.DietasAutorizadas);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task RevocarDieta_no_autorizada_lanza_ReglaDeNegocioException()
    {
        var nutricionista = _banco.Agregar("Ana");
        var handler = new RevocarDietaHandler(_banco.NutricionistasRepo, _banco.UnidadDeTrabajo);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => handler.HandleAsync(new RevocarDietaCommand(nutricionista.Id, Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task RevocarDieta_un_nutricionista_que_no_existe_da_404()
    {
        var handler = new RevocarDietaHandler(_banco.NutricionistasRepo, _banco.UnidadDeTrabajo);

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => handler.HandleAsync(new RevocarDietaCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public void El_validador_de_revocar_exige_la_dieta()
        => Assert.Equal(
            ["DietaId"],
            new RevocarDietaValidator().Validate(new RevocarDietaCommand(Guid.NewGuid(), Guid.Empty))
                .Errors.Select(e => e.PropertyName));

    // ---- AsignarRolJefe ----

    [Fact]
    public async Task AsignarRolJefe_designa_al_nutricionista_como_jefe()
    {
        var nutricionista = _banco.Agregar("Ana");
        var handler = new AsignarRolJefeHandler(_banco.NutricionistasRepo, _banco.UnidadDeTrabajo);

        await handler.HandleAsync(new AsignarRolJefeCommand(nutricionista.Id), CancellationToken.None);

        Assert.True(nutricionista.EsJefe);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task AsignarRolJefe_ya_jefe_es_idempotente()
    {
        var nutricionista = _banco.Agregar("Ana", esJefe: true);
        var handler = new AsignarRolJefeHandler(_banco.NutricionistasRepo, _banco.UnidadDeTrabajo);

        await handler.HandleAsync(new AsignarRolJefeCommand(nutricionista.Id), CancellationToken.None);

        Assert.True(nutricionista.EsJefe);
    }

    [Fact]
    public async Task AsignarRolJefe_un_nutricionista_que_no_existe_da_404()
    {
        var handler = new AsignarRolJefeHandler(_banco.NutricionistasRepo, _banco.UnidadDeTrabajo);

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => handler.HandleAsync(new AsignarRolJefeCommand(Guid.NewGuid()), CancellationToken.None));
    }

    private sealed class IdentidadFalsa(string rol, Guid? entidadId) : IIdentidadAutenticada
    {
        public Guid UsuarioId { get; } = Guid.NewGuid();
        public Guid? EntidadId { get; } = entidadId;
        public string Rol { get; } = rol;
    }
}
