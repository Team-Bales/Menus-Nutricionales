using Menus.Application.Comun;
using Menus.Application.Reportes;
using Menus.Domain.Excepciones;

namespace Menus.Application.Tests.Reportes;

/// <summary>Pruebas de los manejadores de reportes contra un puerto falso.</summary>
public sealed class ManejadoresDeReportesTests
{
    private readonly BancoDeReportesFalso _banco = new();
    private readonly ConsultasFalsas _consultas = new();

    // ---- C1: ListarMenusAutomaticos ----

    [Fact]
    public async Task C1_DietaExistente_DevuelveListaOrdenada()
    {
        var dieta = _banco.AgregarDieta();
        _consultas.MenusAutomaticos = [new MenuAutomaticoDto(Guid.NewGuid(), "Ana", DateTimeOffset.UtcNow, 5, "d", "g")];

        var handler = new ListarMenusAutomaticosHandler(_consultas, _banco.DietasRepo);
        var resultado = await handler.HandleAsync(new ListarMenusAutomaticosQuery(dieta.Id), CancellationToken.None);

        Assert.Single(resultado);
    }

    [Fact]
    public async Task C1_DietaInexistente_LanzaNoEncontrado()
    {
        var handler = new ListarMenusAutomaticosHandler(_consultas, _banco.DietasRepo);
        var id = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() => handler.HandleAsync(new ListarMenusAutomaticosQuery(id), CancellationToken.None));

        Assert.Equal("dieta", ex.Recurso);
        Assert.Equal(id, ex.Id);
    }

    [Fact]
    public async Task C1_AplicaOrdenamiento()
    {
        var dieta = _banco.AgregarDieta();
        var m1 = new MenuAutomaticoDto(Guid.NewGuid(), "Ana", DateTimeOffset.UtcNow.AddDays(-1), 5, "a", "g");
        var m2 = new MenuAutomaticoDto(Guid.NewGuid(), "Berta", DateTimeOffset.UtcNow, 3, "b", "h");
        _consultas.MenusAutomaticos = [m1, m2];

        var handler = new ListarMenusAutomaticosHandler(_consultas, _banco.DietasRepo);
        var resultado = await handler.HandleAsync(new ListarMenusAutomaticosQuery(dieta.Id, new OrdenDeReporte("creador", "asc")), CancellationToken.None);

        Assert.Equal("Ana", resultado[0].Creador);
        Assert.Equal("Berta", resultado[1].Creador);
    }

    // ---- C2: ListarAlimentosMasUsados ----

    [Fact]
    public async Task C2_DietaExistente_DevuelveListaConTop()
    {
        var dieta = _banco.AgregarDieta();
        _consultas.AlimentosMasUsados = [new AlimentoMasUsadoDto(Guid.NewGuid(), "Arroz", "Medio", "Cereales", 10)];

        var handler = new ListarAlimentosMasUsadosHandler(_consultas, _banco.DietasRepo);
        var resultado = await handler.HandleAsync(new ListarAlimentosMasUsadosQuery(dieta.Id, 5), CancellationToken.None);

        Assert.Single(resultado);
    }

    [Fact]
    public async Task C2_DietaInexistente_LanzaNoEncontrado()
    {
        var handler = new ListarAlimentosMasUsadosHandler(_consultas, _banco.DietasRepo);
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<NoEncontradoException>(() => handler.HandleAsync(new ListarAlimentosMasUsadosQuery(id), CancellationToken.None));
    }

    // ---- C3: ListarDictamenes ----

    [Fact]
    public async Task C3_RevisorExistente_DevuelveLista()
    {
        var revisor = _banco.AgregarNutricionista();
        _consultas.Dictamenes = [new DictamenDto(Guid.NewGuid(), "Dieta", "Creador", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, true, "obs", null)];

        var handler = new ListarDictamenesHandler(_consultas, _banco.NutricionistasRepo);
        var resultado = await handler.HandleAsync(new ListarDictamenesQuery(revisor.Id), CancellationToken.None);

        Assert.Single(resultado);
    }

    [Fact]
    public async Task C3_RevisorInexistente_LanzaNoEncontrado()
    {
        var handler = new ListarDictamenesHandler(_consultas, _banco.NutricionistasRepo);
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<NoEncontradoException>(() => handler.HandleAsync(new ListarDictamenesQuery(id), CancellationToken.None));
    }

    // ---- C4: ListarDesempenoMenu ----

    [Fact]
    public async Task C4_MenuExistente_DevuelveLista()
    {
        var menu = _banco.AgregarMenu(Guid.NewGuid(), Guid.NewGuid());
        _consultas.Desempeno = [new DesempenoItemDto(Guid.NewGuid(), "Arroz", "Alimento", "Medio", 10, 8, 2, 0.8m)];

        var handler = new ListarDesempenoMenuHandler(_consultas, _banco.MenusRepo);
        var resultado = await handler.HandleAsync(new ListarDesempenoMenuQuery(menu.Id), CancellationToken.None);

        Assert.Single(resultado);
    }

    [Fact]
    public async Task C4_MenuInexistente_LanzaNoEncontrado()
    {
        var handler = new ListarDesempenoMenuHandler(_consultas, _banco.MenusRepo);
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<NoEncontradoException>(() => handler.HandleAsync(new ListarDesempenoMenuQuery(id), CancellationToken.None));
    }

    // ---- C5: CompararDietas ----

    [Fact]
    public async Task C5_DietasExistentes_DevuelveTresSecciones()
    {
        var d1 = _banco.AgregarDieta();
        var d2 = _banco.AgregarDieta();
        _consultas.ComparacionGrupos = [new ComparacionPorGrupoDto(d1.Id, "D1", "Cereales", 5, 50m)];
        _consultas.ComparacionNiveles = [new ComparacionPorNivelDto(d1.Id, "D1", "Medio", 5, 50m)];
        _consultas.CriteriosEquilibrio = [new CriteriosEquilibrioDto(d1.Id, "D1", Guid.NewGuid(), "Final", 5, 5, true, true, null, true, null, true, null)];

        var handler = new CompararDietasHandler(_consultas, _banco.DietasRepo);
        var resultado = await handler.HandleAsync(new CompararDietasQuery([d1.Id, d2.Id]), CancellationToken.None);

        Assert.Single(resultado.PorGrupo);
        Assert.Single(resultado.PorNivel);
        Assert.Single(resultado.Criterios);
    }

    [Fact]
    public async Task C5_DietaInexistente_LanzaNoEncontrado()
    {
        var d1 = _banco.AgregarDieta();
        var handler = new CompararDietasHandler(_consultas, _banco.DietasRepo);

        await Assert.ThrowsAsync<NoEncontradoException>(() => handler.HandleAsync(new CompararDietasQuery([d1.Id, Guid.NewGuid()]), CancellationToken.None));
    }

    [Fact]
    public async Task C5_OrdenIndependientePorSeccion()
    {
        var d1 = _banco.AgregarDieta();
        var d2 = _banco.AgregarDieta();
        var g1 = new ComparacionPorGrupoDto(d1.Id, "D1", "Cereales", 5, 50m);
        var g2 = new ComparacionPorGrupoDto(d2.Id, "D2", "Frutas", 3, 30m);
        _consultas.ComparacionGrupos = [g2, g1];
        _consultas.ComparacionNiveles = [];
        _consultas.CriteriosEquilibrio = [];

        var handler = new CompararDietasHandler(_consultas, _banco.DietasRepo);
        var resultado = await handler.HandleAsync(new CompararDietasQuery([d1.Id, d2.Id], OrdenGrupos: new OrdenDeReporte("grupoNutricional", "asc")), CancellationToken.None);

        Assert.Equal("Cereales", resultado.PorGrupo[0].GrupoNutricional);
        Assert.Equal("Frutas", resultado.PorGrupo[1].GrupoNutricional);
    }

    // ---- C6: ObtenerCorrelacionNutricional ----

    [Fact]
    public async Task C6_DevuelveTresSecciones()
    {
        _consultas.Correlaciones = [new CorrelacionPorDietaDto(Guid.NewGuid(), "Dieta", 5, 0.8m)];
        _consultas.ItemsRechazados = [new ItemMasRechazadoDto(Guid.NewGuid(), "Arroz", "Alimento", "Ana", 10, 2, 0.2m, "Dieta")];
        _consultas.Revalorizaciones = [new RevalorizadoDto(Guid.NewGuid(), Guid.NewGuid(), "Paciente", "Sala", "Dieta", 5, 3.0m, 2.0m)];

        var handler = new ObtenerCorrelacionNutricionalHandler(_consultas);
        var resultado = await handler.HandleAsync(new ObtenerCorrelacionNutricionalQuery(null, null), CancellationToken.None);

        Assert.Single(resultado.Correlaciones);
        Assert.Single(resultado.ItemsMasRechazados);
        Assert.Single(resultado.Revalorizaciones);
    }

    [Fact]
    public async Task C6_OrdenIndependientePorSeccion()
    {
        var c1 = new CorrelacionPorDietaDto(Guid.NewGuid(), "DietaA", 5, 0.8m);
        var c2 = new CorrelacionPorDietaDto(Guid.NewGuid(), "DietaB", 3, 0.5m);
        _consultas.Correlaciones = [c2, c1];
        _consultas.ItemsRechazados = [];
        _consultas.Revalorizaciones = [];

        var handler = new ObtenerCorrelacionNutricionalHandler(_consultas);
        var resultado = await handler.HandleAsync(new ObtenerCorrelacionNutricionalQuery(null, null, OrdenCorrelacion: new OrdenDeReporte("dieta", "asc")), CancellationToken.None);

        Assert.Equal("DietaA", resultado.Correlaciones[0].Dieta);
        Assert.Equal("DietaB", resultado.Correlaciones[1].Dieta);
    }
}

/// <summary>Puerto falso para las consultas informacionales.</summary>
internal sealed class ConsultasFalsas : IConsultasInformacionales
{
    public IReadOnlyList<MenuAutomaticoDto> MenusAutomaticos { get; set; } = [];
    public IReadOnlyList<AlimentoMasUsadoDto> AlimentosMasUsados { get; set; } = [];
    public IReadOnlyList<DictamenDto> Dictamenes { get; set; } = [];
    public IReadOnlyList<DesempenoItemDto> Desempeno { get; set; } = [];
    public IReadOnlyList<ComparacionPorGrupoDto> ComparacionGrupos { get; set; } = [];
    public IReadOnlyList<ComparacionPorNivelDto> ComparacionNiveles { get; set; } = [];
    public IReadOnlyList<CriteriosEquilibrioDto> CriteriosEquilibrio { get; set; } = [];
    public IReadOnlyList<CorrelacionPorDietaDto> Correlaciones { get; set; } = [];
    public IReadOnlyList<ItemMasRechazadoDto> ItemsRechazados { get; set; } = [];
    public IReadOnlyList<RevalorizadoDto> Revalorizaciones { get; set; } = [];

    public Task<IReadOnlyList<MenuAutomaticoDto>> ConsultarMenusAutomaticosAsync(Guid dietaId, CancellationToken ct = default)
        => Task.FromResult(MenusAutomaticos);

    public Task<IReadOnlyList<AlimentoMasUsadoDto>> ConsultarAlimentosMasUsadosAsync(Guid dietaId, int? limite, CancellationToken ct = default)
        => Task.FromResult(AlimentosMasUsados);

    public Task<IReadOnlyList<DictamenDto>> ConsultarDictamenesAsync(Guid revisorId, CancellationToken ct = default)
        => Task.FromResult(Dictamenes);

    public Task<IReadOnlyList<DesempenoItemDto>> ConsultarDesempenoMenuAsync(Guid menuId, CancellationToken ct = default)
        => Task.FromResult(Desempeno);

    public Task<IReadOnlyList<ComparacionPorGrupoDto>> ConsultarComparacionPorGrupoAsync(IReadOnlyList<Guid> dietas, bool soloFinales, CancellationToken ct = default)
        => Task.FromResult(ComparacionGrupos);

    public Task<IReadOnlyList<ComparacionPorNivelDto>> ConsultarComparacionPorNivelAsync(IReadOnlyList<Guid> dietas, bool soloFinales, CancellationToken ct = default)
        => Task.FromResult(ComparacionNiveles);

    public Task<IReadOnlyList<CriteriosEquilibrioDto>> ConsultarCriteriosEquilibrioAsync(IReadOnlyList<Guid> dietas, bool soloFinales, CancellationToken ct = default)
        => Task.FromResult(CriteriosEquilibrio);

    public Task<IReadOnlyList<CorrelacionPorDietaDto>> ConsultarCorrelacionPorDietaAsync(DateTimeOffset? desde, DateTimeOffset? hasta, CancellationToken ct = default)
        => Task.FromResult(Correlaciones);

    public Task<IReadOnlyList<ItemMasRechazadoDto>> ConsultarItemsMasRechazadosAsync(int limite, int minimoConsumos, CancellationToken ct = default)
        => Task.FromResult(ItemsRechazados);

    public Task<IReadOnlyList<RevalorizadoDto>> ConsultarRevalorizadosAsync(CancellationToken ct = default)
        => Task.FromResult(Revalorizaciones);
}
