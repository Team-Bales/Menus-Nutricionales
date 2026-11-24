using Menus.Api.Auth;
using Menus.Api.Controladores;
using Menus.Application.Comun;
using Menus.Application.Reportes;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Tests.Controladores;

/// <summary>Pruebas de <see cref="ReportesController"/>: qué consulta arma cada acción y qué código devuelve.</summary>
public sealed class ReportesControllerTests
{
    private readonly DespachadorFalso _despachador = new();

    private ReportesController Controlador => new(_despachador);

    private static readonly Guid DietaId = Guid.NewGuid();
    private static readonly Guid RevisorId = Guid.NewGuid();
    private static readonly Guid MenuId = Guid.NewGuid();

    // ---- C1: GET /menus-automaticos ----

    [Fact]
    public async Task GetMenusAutomaticos_ArmaConsultaConDietaIdYSoloOrden()
    {
        var lista = new List<MenuAutomaticoDto> { new(Guid.NewGuid(), "Ana", DateTimeOffset.UtcNow, 5, "d", "g") };
        _despachador.Resultado = lista;

        var resultado = await Controlador.GetMenusAutomaticos(DietaId, "creador", "asc", CancellationToken.None);

        Assert.Same(lista, resultado);
        var query = Assert.IsType<ListarMenusAutomaticosQuery>(_despachador.Recibida);
        Assert.Equal(DietaId, query.DietaId);
        Assert.NotNull(query.Orden);
        Assert.Equal("creador", query.Orden!.Columna);
        Assert.Equal("asc", query.Orden.Direccion);
    }

    [Fact]
    public async Task GetMenusAutomaticos_SinOrden_ArmaConsultaSinOrden()
    {
        var lista = new List<MenuAutomaticoDto> { new(Guid.NewGuid(), "Ana", DateTimeOffset.UtcNow, 5, "d", "g") };
        _despachador.Resultado = lista;

        await Controlador.GetMenusAutomaticos(DietaId, null, null, CancellationToken.None);

        var query = Assert.IsType<ListarMenusAutomaticosQuery>(_despachador.Recibida);
        Assert.Null(query.Orden);
    }

    // ---- C2: GET /alimentos-mas-usados ----

    [Fact]
    public async Task GetAlimentosMasUsados_ArmaConsultaConTopYOrden()
    {
        var lista = new List<AlimentoMasUsadoDto> { new(Guid.NewGuid(), "Arroz", "Medio", "Cereales", 10) };
        _despachador.Resultado = lista;

        var resultado = await Controlador.GetAlimentosMasUsados(DietaId, 5, "alimento", "desc", CancellationToken.None);

        Assert.Same(lista, resultado);
        var query = Assert.IsType<ListarAlimentosMasUsadosQuery>(_despachador.Recibida);
        Assert.Equal(DietaId, query.DietaId);
        Assert.Equal(5, query.Top);
        Assert.NotNull(query.Orden);
        Assert.Equal("alimento", query.Orden!.Columna);
        Assert.Equal("desc", query.Orden.Direccion);
    }

    [Fact]
    public async Task GetAlimentosMasUsados_ValoresPorDefecto_Top10SinOrden()
    {
        var lista = new List<AlimentoMasUsadoDto>();
        _despachador.Resultado = lista;

        await Controlador.GetAlimentosMasUsados(DietaId, ct: CancellationToken.None);

        var query = Assert.IsType<ListarAlimentosMasUsadosQuery>(_despachador.Recibida);
        Assert.Equal(10, query.Top);
        Assert.Null(query.Orden);
    }

    // ---- C3: GET /dictamenes ----

    [Fact]
    public async Task GetDictamenes_ArmaConsultaConRevisorIdYOrden()
    {
        var lista = new List<DictamenDto> { new(Guid.NewGuid(), "Dieta", "Creador", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, true, "obs", null) };
        _despachador.Resultado = lista;

        var resultado = await Controlador.GetDictamenes(RevisorId, "fechaValidacion", "desc", CancellationToken.None);

        Assert.Same(lista, resultado);
        var query = Assert.IsType<ListarDictamenesQuery>(_despachador.Recibida);
        Assert.Equal(RevisorId, query.RevisorId);
        Assert.NotNull(query.Orden);
        Assert.Equal("fechaValidacion", query.Orden!.Columna);
        Assert.Equal("desc", query.Orden.Direccion);
    }

    [Fact]
    public async Task GetDictamenes_SinOrden_ArmaConsultaSinOrden()
    {
        var lista = new List<DictamenDto> { new(Guid.NewGuid(), "Dieta", "Creador", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, true, null, null) };
        _despachador.Resultado = lista;

        await Controlador.GetDictamenes(RevisorId, null, null, CancellationToken.None);

        var query = Assert.IsType<ListarDictamenesQuery>(_despachador.Recibida);
        Assert.Equal(RevisorId, query.RevisorId);
        Assert.Null(query.Orden);
    }

    // ---- C4: GET /desempeno-menu ----

    [Fact]
    public async Task GetDesempenoMenu_ArmaConsultaConMenuIdYOrden()
    {
        var lista = new List<DesempenoItemDto> { new(Guid.NewGuid(), "Arroz", "Alimento", "Medio", 10, 8, 2, 0.8m) };
        _despachador.Resultado = lista;

        var resultado = await Controlador.GetDesempenoMenu(MenuId, "tasaAceptacion", "desc", CancellationToken.None);

        Assert.Same(lista, resultado);
        var query = Assert.IsType<ListarDesempenoMenuQuery>(_despachador.Recibida);
        Assert.Equal(MenuId, query.MenuId);
        Assert.NotNull(query.Orden);
        Assert.Equal("tasaAceptacion", query.Orden!.Columna);
        Assert.Equal("desc", query.Orden.Direccion);
    }

    [Fact]
    public async Task GetDesempenoMenu_SinOrden_ArmaConsultaSinOrden()
    {
        var lista = new List<DesempenoItemDto> { new(Guid.NewGuid(), "Arroz", "Alimento", "Medio", 10, 8, 2, 0.8m) };
        _despachador.Resultado = lista;

        await Controlador.GetDesempenoMenu(MenuId, null, null, CancellationToken.None);

        var query = Assert.IsType<ListarDesempenoMenuQuery>(_despachador.Recibida);
        Assert.Equal(MenuId, query.MenuId);
        Assert.Null(query.Orden);
    }

    // ---- C5: GET /comparacion-dietas ----

    [Fact]
    public async Task GetComparacionDietas_ArmaConsultaConTresOrdenesIndependientes()
    {
        var dietas = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var dto = new ComparacionDietasDto([], [], []);
        _despachador.Resultado = dto;

        var resultado = await Controlador.GetComparacionDietas(
            dietas, true,
            "grupoNutricional", "asc",
            "nivelCalorico", "desc",
            "estado", "asc",
            CancellationToken.None);

        Assert.Same(dto, resultado);
        var query = Assert.IsType<CompararDietasQuery>(_despachador.Recibida);
        Assert.Equal(dietas, query.Dietas);
        Assert.True(query.SoloFinales);
        Assert.NotNull(query.OrdenGrupos);
        Assert.Equal("grupoNutricional", query.OrdenGrupos!.Columna);
        Assert.Equal("asc", query.OrdenGrupos.Direccion);
        Assert.NotNull(query.OrdenNiveles);
        Assert.Equal("nivelCalorico", query.OrdenNiveles!.Columna);
        Assert.Equal("desc", query.OrdenNiveles.Direccion);
        Assert.NotNull(query.OrdenEquilibrio);
        Assert.Equal("estado", query.OrdenEquilibrio!.Columna);
        Assert.Equal("asc", query.OrdenEquilibrio.Direccion);
    }

    [Fact]
    public async Task GetComparacionDietas_ValoresPorDefecto_SoloFinalesFalseSinOrdenes()
    {
        var dietas = new[] { Guid.NewGuid(), Guid.NewGuid() };
        _despachador.Resultado = new ComparacionDietasDto([], [], []);

        await Controlador.GetComparacionDietas(dietas, ct: CancellationToken.None);

        var query = Assert.IsType<CompararDietasQuery>(_despachador.Recibida);
        Assert.False(query.SoloFinales);
        Assert.Null(query.OrdenGrupos);
        Assert.Null(query.OrdenNiveles);
        Assert.Null(query.OrdenEquilibrio);
    }

    [Fact]
    public async Task GetComparacionDietas_BindingDietasArray_RecibeMultiplesGuid()
    {
        var dietas = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        _despachador.Resultado = new ComparacionDietasDto([], [], []);

        await Controlador.GetComparacionDietas(dietas, ct: CancellationToken.None);

        var query = Assert.IsType<CompararDietasQuery>(_despachador.Recibida);
        Assert.Equal(3, query.Dietas.Count);
        Assert.Equal(dietas, query.Dietas);
    }

    [Fact]
    public async Task GetComparacionDietas_SoloOrdenGrupos_OtrosNulos()
    {
        var dietas = new[] { Guid.NewGuid(), Guid.NewGuid() };
        _despachador.Resultado = new ComparacionDietasDto([], [], []);

        await Controlador.GetComparacionDietas(
            dietas, false,
            "dieta", "asc",
            null, null,
            null, null,
            CancellationToken.None);

        var query = Assert.IsType<CompararDietasQuery>(_despachador.Recibida);
        Assert.NotNull(query.OrdenGrupos);
        Assert.Null(query.OrdenNiveles);
        Assert.Null(query.OrdenEquilibrio);
    }

    // ---- C6: GET /correlacion-nutricional ----

    [Fact]
    public async Task GetCorrelacionNutricional_ArmaConsultaConParametrosYTresOrdenes()
    {
        var desde = DateTimeOffset.UtcNow.AddDays(-7);
        var hasta = DateTimeOffset.UtcNow;
        var dto = new CorrelacionNutricionalDto([], [], []);
        _despachador.Resultado = dto;

        var resultado = await Controlador.GetCorrelacionNutricional(
            desde, hasta, 2,
            "dieta", "asc",
            "tasaRechazo", "desc",
            "paciente", "asc",
            CancellationToken.None);

        Assert.Same(dto, resultado);
        var query = Assert.IsType<ObtenerCorrelacionNutricionalQuery>(_despachador.Recibida);
        Assert.Equal(desde, query.Desde);
        Assert.Equal(hasta, query.Hasta);
        Assert.Equal(2, query.MinimoConsumos);
        Assert.NotNull(query.OrdenCorrelacion);
        Assert.Equal("dieta", query.OrdenCorrelacion!.Columna);
        Assert.Equal("asc", query.OrdenCorrelacion.Direccion);
        Assert.NotNull(query.OrdenRechazos);
        Assert.Equal("tasaRechazo", query.OrdenRechazos!.Columna);
        Assert.Equal("desc", query.OrdenRechazos.Direccion);
        Assert.NotNull(query.OrdenRevalorizaciones);
        Assert.Equal("paciente", query.OrdenRevalorizaciones!.Columna);
        Assert.Equal("asc", query.OrdenRevalorizaciones.Direccion);
    }

    [Fact]
    public async Task GetCorrelacionNutricional_ValoresPorDefecto_MinimoConsumos1SinOrdenes()
    {
        _despachador.Resultado = new CorrelacionNutricionalDto([], [], []);

        await Controlador.GetCorrelacionNutricional(null, null, ct: CancellationToken.None);

        var query = Assert.IsType<ObtenerCorrelacionNutricionalQuery>(_despachador.Recibida);
        Assert.Equal(1, query.MinimoConsumos);
        Assert.Null(query.OrdenCorrelacion);
        Assert.Null(query.OrdenRechazos);
        Assert.Null(query.OrdenRevalorizaciones);
    }

    [Fact]
    public async Task GetCorrelacionNutricional_SoloDesdeHasta_SinOrdenes()
    {
        var desde = DateTimeOffset.UtcNow.AddDays(-30);
        var hasta = DateTimeOffset.UtcNow;
        _despachador.Resultado = new CorrelacionNutricionalDto([], [], []);

        await Controlador.GetCorrelacionNutricional(desde, hasta, ct: CancellationToken.None);

        var query = Assert.IsType<ObtenerCorrelacionNutricionalQuery>(_despachador.Recibida);
        Assert.Equal(desde, query.Desde);
        Assert.Equal(hasta, query.Hasta);
        Assert.Equal(1, query.MinimoConsumos);
        Assert.Null(query.OrdenCorrelacion);
        Assert.Null(query.OrdenRechazos);
        Assert.Null(query.OrdenRevalorizaciones);
    }

    [Fact]
    public async Task GetCorrelacionNutricional_SoloOrdenCorrelacion_OtrosNulos()
    {
        _despachador.Resultado = new CorrelacionNutricionalDto([], [], []);

        await Controlador.GetCorrelacionNutricional(
            null, null, 5,
            "menusValorados", "desc",
            null, null,
            null, null,
            CancellationToken.None);

        var query = Assert.IsType<ObtenerCorrelacionNutricionalQuery>(_despachador.Recibida);
        Assert.Equal(5, query.MinimoConsumos);
        Assert.NotNull(query.OrdenCorrelacion);
        Assert.Equal("menusValorados", query.OrdenCorrelacion!.Columna);
        Assert.Equal("desc", query.OrdenCorrelacion.Direccion);
        Assert.Null(query.OrdenRechazos);
        Assert.Null(query.OrdenRevalorizaciones);
    }

    /// <summary>Guarda la solicitud recibida y devuelve el resultado preparado.</summary>
    private sealed class DespachadorFalso : IDespachador
    {
        public object? Resultado { get; set; }
        public object? Recibida { get; private set; }

        public Task<TResultado> EnviarAsync<TResultado>(ISolicitud<TResultado> solicitud, CancellationToken ct = default)
        {
            Recibida = solicitud;
            return Task.FromResult((TResultado)Resultado!);
        }
    }
}
