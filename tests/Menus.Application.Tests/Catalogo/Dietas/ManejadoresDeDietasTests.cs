using Menus.Application.Catalogo.Dietas.ActualizarDieta;
using Menus.Application.Catalogo.Dietas.AgregarGrupoDieta;
using Menus.Application.Catalogo.Dietas.AgregarRestriccionDieta;
using Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;
using Menus.Application.Catalogo.Dietas.EliminarDieta;
using Menus.Application.Catalogo.Dietas.ListarDietas;
using Menus.Application.Catalogo.Dietas.ObtenerDieta;
using Menus.Application.Catalogo.Dietas.QuitarGrupoDieta;
using Menus.Application.Catalogo.Dietas.QuitarRestriccionDieta;
using Menus.Application.Catalogo.Dietas.RegistrarDieta;
using Menus.Application.Comun;
using Menus.Application.Tests.Comun;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Catalogo.Dietas;

/// <summary>Pruebas de los manejadores del #63 contra el catálogo en memoria.</summary>
public sealed class ManejadoresDeDietasTests
{
    private readonly BancoDeDietasFalso _banco = new();
    private readonly CacheFalsa _cache = new();

    private RegistrarDietaHandler Registrar() => new(_banco.DietasRepo, _banco.UnidadDeTrabajo);

    private ActualizarDietaHandler Actualizar() => new(_banco.DietasRepo, _banco.UnidadDeTrabajo);

    private AgregarGrupoDietaHandler AgregarGrupo() => new(_banco.DietasRepo, _banco.UnidadDeTrabajo, _cache);

    private QuitarGrupoDietaHandler QuitarGrupo() => new(_banco.DietasRepo, _banco.UnidadDeTrabajo, _cache);

    private AgregarRestriccionDietaHandler AgregarRestriccion() => new(_banco.DietasRepo, _banco.UnidadDeTrabajo, _cache);

    private QuitarRestriccionDietaHandler QuitarRestriccion() => new(_banco.DietasRepo, _banco.UnidadDeTrabajo, _cache);

    private EliminarDietaHandler Eliminar() => new(_banco.DietasRepo, _banco.PacientesRepo, _banco.MenusRepo, _banco.UnidadDeTrabajo);

    // ---- Registrar ----

    [Fact]
    public async Task Registrar_guarda_la_dieta_con_su_cobertura_y_restricciones_y_confirma_una_vez()
    {
        var comando = new RegistrarDietaCommand(
            "  Dieta renal  ",
            BancoDeDietasFalso.ProgramaPediatria,
            [new GrupoACubrir(BancoDeDietasFalso.GrupoCereales, TipoCobertura.Requerido)],
            [BancoDeDietasFalso.RestriccionSinGluten]);

        var creada = await Registrar().HandleAsync(comando, CancellationToken.None);

        var dieta = Assert.Single(_banco.Dietas);
        Assert.Equal(creada.Id, dieta.Id);
        Assert.Equal("Dieta renal", dieta.Nombre);
        Assert.Equal(BancoDeDietasFalso.ProgramaPediatria, dieta.ProgramaAtencionId);
        Assert.Equal([BancoDeDietasFalso.GrupoCereales], dieta.GruposNutricionales.Select(g => g.GrupoNutricionalId));
        Assert.Equal([BancoDeDietasFalso.RestriccionSinGluten], dieta.Restricciones.Select(r => r.RestriccionAlimentariaId));
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    // ---- Actualizar ----

    [Fact]
    public async Task Actualizar_cambia_nombre_y_programa_y_confirma()
    {
        var dieta = _banco.Agregar("Dieta vieja");
        var comando = new ActualizarDietaCommand(dieta.Id, "Dieta nueva", BancoDeDietasFalso.ProgramaOncologia);

        var resultado = await Actualizar().HandleAsync(comando, CancellationToken.None);

        Assert.Same(Unidad.Valor, resultado);
        Assert.Equal("Dieta nueva", dieta.Nombre);
        Assert.Equal(BancoDeDietasFalso.ProgramaOncologia, dieta.ProgramaAtencionId);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_una_dieta_que_no_existe_lanza_NoEncontrado()
    {
        var comando = new ActualizarDietaCommand(Guid.NewGuid(), "X", BancoDeDietasFalso.ProgramaPediatria);

        await Assert.ThrowsAsync<NoEncontradoException>(() => Actualizar().HandleAsync(comando, CancellationToken.None));
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    // ---- Agregar / quitar grupo ----

    [Fact]
    public async Task AgregarGrupo_nuevo_lo_cubre_y_confirma()
    {
        var dieta = _banco.Agregar("Dieta");
        var comando = new AgregarGrupoDietaCommand(dieta.Id, BancoDeDietasFalso.GrupoFrutas, TipoCobertura.Restringido);

        await AgregarGrupo().HandleAsync(comando, CancellationToken.None);

        Assert.Contains(dieta.GruposNutricionales, g => g.GrupoNutricionalId == BancoDeDietasFalso.GrupoFrutas);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task AgregarGrupo_ya_cubierto_no_duplica_ni_confirma()
    {
        var dieta = _banco.Agregar("Dieta"); // ya cubre GrupoCereales
        var comando = new AgregarGrupoDietaCommand(dieta.Id, BancoDeDietasFalso.GrupoCereales, TipoCobertura.Requerido);

        await AgregarGrupo().HandleAsync(comando, CancellationToken.None);

        Assert.Single(dieta.GruposNutricionales);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task AgregarGrupo_a_una_dieta_que_no_existe_lanza_NoEncontrado()
    {
        var comando = new AgregarGrupoDietaCommand(Guid.NewGuid(), BancoDeDietasFalso.GrupoFrutas, TipoCobertura.Requerido);

        await Assert.ThrowsAsync<NoEncontradoException>(() => AgregarGrupo().HandleAsync(comando, CancellationToken.None));
    }

    [Fact]
    public async Task QuitarGrupo_existente_lo_quita_y_confirma()
    {
        var dieta = _banco.Agregar("Dieta");
        dieta.AgregarGrupo(BancoDeDietasFalso.GrupoFrutas, TipoCobertura.Restringido);

        await QuitarGrupo().HandleAsync(new QuitarGrupoDietaCommand(dieta.Id, BancoDeDietasFalso.GrupoFrutas), CancellationToken.None);

        Assert.DoesNotContain(dieta.GruposNutricionales, g => g.GrupoNutricionalId == BancoDeDietasFalso.GrupoFrutas);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task QuitarGrupo_no_cubierto_no_confirma()
    {
        var dieta = _banco.Agregar("Dieta");

        await QuitarGrupo().HandleAsync(new QuitarGrupoDietaCommand(dieta.Id, BancoDeDietasFalso.GrupoFrutas), CancellationToken.None);

        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task QuitarGrupo_el_unico_requerido_lanza_RK07_sin_confirmar()
    {
        var dieta = _banco.Agregar("Dieta"); // su único grupo, GrupoCereales, es Requerido

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => QuitarGrupo().HandleAsync(new QuitarGrupoDietaCommand(dieta.Id, BancoDeDietasFalso.GrupoCereales), CancellationToken.None));

        Assert.Equal("RK-07", ex.Regla);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    // ---- Agregar / quitar restricción ----

    [Fact]
    public async Task AgregarRestriccion_nueva_la_agrega_y_confirma()
    {
        var dieta = _banco.Agregar("Dieta");

        await AgregarRestriccion().HandleAsync(
            new AgregarRestriccionDietaCommand(dieta.Id, BancoDeDietasFalso.RestriccionSinGluten), CancellationToken.None);

        Assert.Contains(dieta.Restricciones, r => r.RestriccionAlimentariaId == BancoDeDietasFalso.RestriccionSinGluten);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task AgregarRestriccion_ya_presente_no_duplica_ni_confirma()
    {
        var dieta = _banco.Agregar("Dieta");
        dieta.AgregarRestriccion(BancoDeDietasFalso.RestriccionSinGluten);

        await AgregarRestriccion().HandleAsync(
            new AgregarRestriccionDietaCommand(dieta.Id, BancoDeDietasFalso.RestriccionSinGluten), CancellationToken.None);

        Assert.Single(dieta.Restricciones);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task QuitarRestriccion_existente_la_quita_y_confirma()
    {
        var dieta = _banco.Agregar("Dieta");
        dieta.AgregarRestriccion(BancoDeDietasFalso.RestriccionSinGluten);

        await QuitarRestriccion().HandleAsync(
            new QuitarRestriccionDietaCommand(dieta.Id, BancoDeDietasFalso.RestriccionSinGluten), CancellationToken.None);

        Assert.Empty(dieta.Restricciones);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task QuitarRestriccion_no_presente_no_confirma()
    {
        var dieta = _banco.Agregar("Dieta");

        await QuitarRestriccion().HandleAsync(
            new QuitarRestriccionDietaCommand(dieta.Id, BancoDeDietasFalso.RestriccionSinGluten), CancellationToken.None);

        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    // ---- Eliminar ----

    [Fact]
    public async Task Eliminar_una_dieta_sin_uso_la_quita_y_confirma()
    {
        var dieta = _banco.Agregar("Dieta");

        await Eliminar().HandleAsync(new EliminarDietaCommand(dieta.Id), CancellationToken.None);

        Assert.Empty(_banco.Dietas);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Eliminar_una_dieta_que_no_existe_lanza_NoEncontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(
            () => Eliminar().HandleAsync(new EliminarDietaCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Eliminar_una_dieta_con_pacientes_inscritos_lanza_RR12_sin_quitarla()
    {
        var dieta = _banco.Agregar("Dieta");
        _banco.InscribirPaciente(dieta.Id);

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Eliminar().HandleAsync(new EliminarDietaCommand(dieta.Id), CancellationToken.None));

        Assert.Equal("RR-12", ex.Regla);
        Assert.Single(_banco.Dietas);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Eliminar_una_dieta_con_menus_lanza_RR13_sin_quitarla()
    {
        var dieta = _banco.Agregar("Dieta");
        _banco.CrearMenu(dieta.Id);

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Eliminar().HandleAsync(new EliminarDietaCommand(dieta.Id), CancellationToken.None));

        Assert.Equal("RR-13", ex.Regla);
        Assert.Single(_banco.Dietas);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    // ---- Consultas ----

    [Fact]
    public async Task Listar_devuelve_la_pagina_pedida_con_el_total_filtrado()
    {
        _banco.Agregar("Dieta A", programa: BancoDeDietasFalso.ProgramaPediatria);
        _banco.Agregar("Dieta B", programa: BancoDeDietasFalso.ProgramaPediatria);
        _banco.Agregar("Dieta C", programa: BancoDeDietasFalso.ProgramaPediatria);
        _banco.Agregar("Dieta D", programa: BancoDeDietasFalso.ProgramaOncologia);

        var resultado = await new ListarDietasHandler(_banco.DietasRepo).HandleAsync(
            new ListarDietasQuery(ProgramaAtencionId: BancoDeDietasFalso.ProgramaPediatria, Page: 2, Size: 2),
            CancellationToken.None);

        Assert.Equal(3, resultado.Total);
        Assert.Equal(2, resultado.Page);
        Assert.Equal(2, resultado.Size);
        Assert.Equal("Dieta C", Assert.Single(resultado.Data).Nombre);
    }

    [Fact]
    public async Task Obtener_devuelve_el_dto_con_grupos_y_restricciones_expandidos()
    {
        var dieta = _banco.Agregar("Dieta");
        dieta.AgregarGrupo(BancoDeDietasFalso.GrupoFrutas, TipoCobertura.Restringido);
        dieta.AgregarRestriccion(BancoDeDietasFalso.RestriccionSinGluten);

        var dto = await new ObtenerDietaHandler(_banco.DietasRepo).HandleAsync(
            new ObtenerDietaQuery(dieta.Id), CancellationToken.None);

        Assert.Equal(dieta.Id, dto.Id);
        Assert.Equal("Dieta", dto.Nombre);
        Assert.Equal(2, dto.Grupos.Count);
        Assert.Contains(dto.Grupos, g => g.GrupoNutricionalId == BancoDeDietasFalso.GrupoCereales && g.Tipo == TipoCobertura.Requerido);
        Assert.Contains(dto.Grupos, g => g.GrupoNutricionalId == BancoDeDietasFalso.GrupoFrutas && g.Tipo == TipoCobertura.Restringido);
        Assert.Equal([BancoDeDietasFalso.RestriccionSinGluten], dto.Restricciones);
    }

    [Fact]
    public async Task Obtener_una_dieta_que_no_existe_lanza_NoEncontrado()
    {
        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() => new ObtenerDietaHandler(_banco.DietasRepo)
            .HandleAsync(new ObtenerDietaQuery(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("dieta", ex.Recurso);
    }

    // ---- Invalidación de la disponibilidad calórica (#101) ----

    [Fact]
    public async Task AgregarGrupo_invalida_la_disponibilidad_calorica_de_la_dieta()
    {
        var dieta = _banco.Agregar("Dieta");
        var clave = CacheDeDisponibilidadCalorica.Clave(dieta.Id);
        await _cache.GuardarAsync(clave, "valor-viejo", TimeSpan.FromHours(1));

        await AgregarGrupo().HandleAsync(
            new AgregarGrupoDietaCommand(dieta.Id, BancoDeDietasFalso.GrupoFrutas, TipoCobertura.Restringido),
            CancellationToken.None);

        Assert.Null(await _cache.ObtenerAsync<string>(clave, CancellationToken.None));
    }

    [Fact]
    public async Task AgregarGrupo_ya_cubierto_no_invalida_nada_porque_no_cambia()
    {
        var dieta = _banco.Agregar("Dieta");
        var clave = CacheDeDisponibilidadCalorica.Clave(dieta.Id);
        await _cache.GuardarAsync(clave, "valor-vigente", TimeSpan.FromHours(1));

        await AgregarGrupo().HandleAsync(
            new AgregarGrupoDietaCommand(dieta.Id, BancoDeDietasFalso.GrupoCereales, TipoCobertura.Requerido),
            CancellationToken.None);

        Assert.Equal("valor-vigente", await _cache.ObtenerAsync<string>(clave, CancellationToken.None));
    }

    [Fact]
    public async Task QuitarRestriccion_invalida_la_disponibilidad_calorica_de_la_dieta()
    {
        var dieta = _banco.Agregar("Dieta");
        dieta.AgregarRestriccion(BancoDeDietasFalso.RestriccionSinGluten);
        var clave = CacheDeDisponibilidadCalorica.Clave(dieta.Id);
        await _cache.GuardarAsync(clave, "valor-viejo", TimeSpan.FromHours(1));

        await QuitarRestriccion().HandleAsync(
            new QuitarRestriccionDietaCommand(dieta.Id, BancoDeDietasFalso.RestriccionSinGluten),
            CancellationToken.None);

        Assert.Null(await _cache.ObtenerAsync<string>(clave, CancellationToken.None));
    }
}
