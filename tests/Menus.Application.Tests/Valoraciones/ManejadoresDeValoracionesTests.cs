using Menus.Application.Comun;
using Menus.Application.Tests.Catalogo;
using Menus.Application.Tests.Catalogo.Dietas;
using Menus.Application.Tests.Comun;
using Menus.Application.Valoraciones;
using Menus.Application.Valoraciones.ListarValoracionesPorDieta;
using Menus.Application.Valoraciones.ListarValoracionesPorPaciente;
using Menus.Application.Valoraciones.ObtenerValoracion;
using Menus.Application.Valoraciones.RegistrarValoracion;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Valoraciones;

/// <summary>Pruebas de los manejadores del #86 contra los datos en memoria.</summary>
public sealed class ManejadoresDeValoracionesTests
{
    private readonly BancoDeDietasFalso _banco = new();
    private readonly ValoracionesFalsas _valoraciones;
    private readonly CacheFalsa _cache = new();

    public ManejadoresDeValoracionesTests() => _valoraciones = new ValoracionesFalsas(_banco);

    private RegistrarValoracionHandler Registrar(Guid nutricionistaId)
        => new(_banco.MenusRepo, _valoraciones, _banco.UnidadDeTrabajo, new ActorFalso(nutricionistaId), _cache);

    private ObtenerValoracionHandler Obtener() => new(_valoraciones);

    private ListarValoracionesPorDietaHandler ListarPorDieta() => new(_banco.DietasRepo, _valoraciones);

    private ListarValoracionesPorPacienteHandler ListarPorPaciente() => new(_banco.PacientesRepo, _valoraciones, _cache);

    /// <summary>Un menú final de la dieta, ya asignado al paciente (la asignación que evalúa la valoración).</summary>
    private Menu NuevoMenuAsignado(Guid dietaId, Guid pacienteId, DateTime? fechaCreacion = null)
    {
        var menu = new Menu(
            Guid.NewGuid(), dietaId, Guid.NewGuid(), fechaCreacion ?? DateTime.UtcNow.AddDays(-2), 1, true,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)], [BancoDeDietasFalso.GrupoCereales]);
        menu.Aprobar(Guid.NewGuid(), menu.FechaCreacion.AddHours(1));
        menu.AsignarPaciente(pacienteId);
        _banco.Menus.Add(menu);
        return menu;
    }

    // ---- Registrar ----

    [Fact]
    public async Task Registrar_crea_la_valoracion_con_el_actor_como_nutricionista_y_confirma()
    {
        var dieta = _banco.Agregar("Dieta renal");
        var paciente = new Paciente(Guid.NewGuid(), "Ana", 30, Guid.NewGuid());
        _banco.Pacientes.Add(paciente);
        var menu = NuevoMenuAsignado(dieta.Id, paciente.Id);
        var nutricionistaId = Guid.NewGuid();

        var creada = await Registrar(nutricionistaId).HandleAsync(
            new RegistrarValoracionCommand(menu.Id, paciente.Id, ResultadoNutricional.Buena.Id),
            CancellationToken.None);

        var valoracion = Assert.Single(_valoraciones.Valoraciones);
        Assert.Equal(creada.Id, valoracion.Id);
        Assert.Equal(menu.Id, valoracion.MenuId);
        Assert.Equal(paciente.Id, valoracion.PacienteId);
        Assert.Equal(nutricionistaId, valoracion.NutricionistaId);
        Assert.Equal(ResultadoNutricional.Buena.Id, valoracion.ResultadoNutricionalId);
        Assert.Equal(DateTimeKind.Utc, valoracion.FechaValoracion.Kind);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Registrar_sobre_un_menu_que_no_existe_es_404()
    {
        var menuInexistente = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(() => Registrar(Guid.NewGuid()).HandleAsync(
            new RegistrarValoracionCommand(menuInexistente, Guid.NewGuid(), ResultadoNutricional.Buena.Id),
            CancellationToken.None));

        Assert.Equal("menú", error.Recurso);
        Assert.Equal(menuInexistente, error.Id);
        Assert.Empty(_valoraciones.Valoraciones);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Registrar_sin_que_el_paciente_tenga_el_menu_asignado_es_404()
    {
        var dieta = _banco.Agregar("Dieta renal");
        var otroPaciente = Guid.NewGuid();
        var menu = NuevoMenuAsignado(dieta.Id, otroPaciente);
        var pacienteSinAsignar = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(() => Registrar(Guid.NewGuid()).HandleAsync(
            new RegistrarValoracionCommand(menu.Id, pacienteSinAsignar, ResultadoNutricional.Buena.Id),
            CancellationToken.None));

        Assert.Equal("asignación", error.Recurso);
        Assert.Empty(_valoraciones.Valoraciones);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    // ---- Obtener ----

    [Fact]
    public async Task Obtener_devuelve_el_resultado_expandido()
    {
        var valoracion = new ValoracionNutricional(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ResultadoNutricional.Excelente.Id, DateTime.UtcNow);
        _valoraciones.Valoraciones.Add(valoracion);

        var detalle = await Obtener().HandleAsync(new ObtenerValoracionQuery(valoracion.Id), CancellationToken.None);

        Assert.Equal(valoracion.Id, detalle.Id);
        Assert.Equal(valoracion.MenuId, detalle.MenuId);
        Assert.Equal(valoracion.PacienteId, detalle.PacienteId);
        Assert.Equal(valoracion.NutricionistaId, detalle.NutricionistaId);
        Assert.Equal(ResultadoNutricional.Excelente.Id, detalle.Resultado.Id);
        Assert.Equal("Excelente", detalle.Resultado.Nombre);
        Assert.Equal(5, detalle.Resultado.Valor);
    }

    [Fact]
    public async Task Obtener_una_valoracion_que_no_existe_es_404()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Obtener().HandleAsync(new ObtenerValoracionQuery(id), CancellationToken.None));

        Assert.Equal("valoración", error.Recurso);
        Assert.Equal(id, error.Id);
    }

    // ---- ListarPorDieta ----

    [Fact]
    public async Task ListarPorDieta_devuelve_las_de_todos_sus_pacientes_de_la_mas_reciente_a_la_mas_antigua()
    {
        var renal = _banco.Agregar("Dieta renal");
        var otra = _banco.Agregar("Dieta blanda");
        var ana = new Paciente(Guid.NewGuid(), "Ana", 30, Guid.NewGuid());
        var beto = new Paciente(Guid.NewGuid(), "Beto", 40, Guid.NewGuid());
        _banco.Pacientes.AddRange([ana, beto]);
        var menuDeAna = NuevoMenuAsignado(renal.Id, ana.Id, DateTime.UtcNow.AddDays(-10));
        var menuDeBeto = NuevoMenuAsignado(renal.Id, beto.Id, DateTime.UtcNow.AddDays(-9));
        var menuDeOtraDieta = NuevoMenuAsignado(otra.Id, ana.Id, DateTime.UtcNow.AddDays(-8));

        var antigua = new ValoracionNutricional(
            Guid.NewGuid(), menuDeAna.Id, ana.Id, Guid.NewGuid(), ResultadoNutricional.Normal.Id,
            DateTime.UtcNow.AddHours(-2));
        var reciente = new ValoracionNutricional(
            Guid.NewGuid(), menuDeBeto.Id, beto.Id, Guid.NewGuid(), ResultadoNutricional.Buena.Id,
            DateTime.UtcNow.AddHours(-1));
        var deOtraDieta = new ValoracionNutricional(
            Guid.NewGuid(), menuDeOtraDieta.Id, ana.Id, Guid.NewGuid(), ResultadoNutricional.MuyMala.Id,
            DateTime.UtcNow);
        _valoraciones.Valoraciones.AddRange([antigua, reciente, deOtraDieta]);

        var resultado = await ListarPorDieta().HandleAsync(new ListarValoracionesPorDietaQuery(renal.Id), CancellationToken.None);

        Assert.Equal([reciente.Id, antigua.Id], resultado.Select(v => v.Id));
    }

    [Fact]
    public async Task ListarPorDieta_de_una_dieta_que_no_existe_es_404()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(() => ListarPorDieta().HandleAsync(
            new ListarValoracionesPorDietaQuery(id), CancellationToken.None));

        Assert.Equal("dieta", error.Recurso);
        Assert.Equal(id, error.Id);
    }

    // ---- ListarPorPaciente ----

    [Fact]
    public async Task ListarPorPaciente_devuelve_las_de_todas_sus_dietas_de_la_mas_reciente_a_la_mas_antigua()
    {
        var renal = _banco.Agregar("Dieta renal");
        var blanda = _banco.Agregar("Dieta blanda");
        var ana = new Paciente(Guid.NewGuid(), "Ana", 30, Guid.NewGuid());
        var otroPaciente = new Paciente(Guid.NewGuid(), "Beto", 40, Guid.NewGuid());
        _banco.Pacientes.AddRange([ana, otroPaciente]);
        var menuRenal = NuevoMenuAsignado(renal.Id, ana.Id, DateTime.UtcNow.AddDays(-10));
        var menuBlanda = NuevoMenuAsignado(blanda.Id, ana.Id, DateTime.UtcNow.AddDays(-9));
        var menuDeOtro = NuevoMenuAsignado(renal.Id, otroPaciente.Id, DateTime.UtcNow.AddDays(-8));

        var antigua = new ValoracionNutricional(
            Guid.NewGuid(), menuRenal.Id, ana.Id, Guid.NewGuid(), ResultadoNutricional.Normal.Id,
            DateTime.UtcNow.AddHours(-2));
        var reciente = new ValoracionNutricional(
            Guid.NewGuid(), menuBlanda.Id, ana.Id, Guid.NewGuid(), ResultadoNutricional.Buena.Id,
            DateTime.UtcNow.AddHours(-1));
        var deOtroPaciente = new ValoracionNutricional(
            Guid.NewGuid(), menuDeOtro.Id, otroPaciente.Id, Guid.NewGuid(), ResultadoNutricional.MuyMala.Id,
            DateTime.UtcNow);
        _valoraciones.Valoraciones.AddRange([antigua, reciente, deOtroPaciente]);

        var resultado = await ListarPorPaciente().HandleAsync(
            new ListarValoracionesPorPacienteQuery(ana.Id), CancellationToken.None);

        Assert.Equal([reciente.Id, antigua.Id], resultado.Select(v => v.Id));
    }

    [Fact]
    public async Task ListarPorPaciente_de_un_paciente_que_no_existe_es_404()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(() => ListarPorPaciente().HandleAsync(
            new ListarValoracionesPorPacienteQuery(id), CancellationToken.None));

        Assert.Equal("paciente", error.Recurso);
        Assert.Equal(id, error.Id);
    }

    // ---- Caché (#101) ----

    [Fact]
    public async Task ListarPorPaciente_guarda_el_resultado_en_cache()
    {
        var paciente = new Paciente(Guid.NewGuid(), "Ana", 30, Guid.NewGuid());
        _banco.Pacientes.Add(paciente);

        await ListarPorPaciente().HandleAsync(new ListarValoracionesPorPacienteQuery(paciente.Id), CancellationToken.None);

        Assert.Equal(1, _cache.Escrituras);
    }

    [Fact]
    public async Task ListarPorPaciente_devuelve_el_valor_cacheado_sin_tocar_los_repositorios()
    {
        var pacienteId = Guid.NewGuid();
        var cacheada = new List<ValoracionDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), pacienteId, Guid.NewGuid(),
                new ResultadoNutricionalDto(ResultadoNutricional.Buena.Id, "Buena", 4), DateTime.UtcNow),
        };
        await _cache.GuardarAsync(
            CacheDeValoracionesRecientes.Clave(pacienteId), cacheada, TimeSpan.FromHours(1));

        var resultado = await ListarPorPaciente().HandleAsync(
            new ListarValoracionesPorPacienteQuery(pacienteId), CancellationToken.None);

        Assert.Same(cacheada, resultado);
    }

    [Fact]
    public async Task Registrar_invalida_la_cache_de_valoraciones_recientes_del_paciente()
    {
        var dieta = _banco.Agregar("Dieta renal");
        var paciente = new Paciente(Guid.NewGuid(), "Ana", 30, Guid.NewGuid());
        _banco.Pacientes.Add(paciente);
        var menu = NuevoMenuAsignado(dieta.Id, paciente.Id);
        var clave = CacheDeValoracionesRecientes.Clave(paciente.Id);
        await _cache.GuardarAsync(clave, new List<ValoracionDto>(), TimeSpan.FromHours(1));

        await Registrar(Guid.NewGuid()).HandleAsync(
            new RegistrarValoracionCommand(menu.Id, paciente.Id, ResultadoNutricional.Buena.Id), CancellationToken.None);

        Assert.Null(await _cache.ObtenerAsync<List<ValoracionDto>>(clave, CancellationToken.None));
    }
}
