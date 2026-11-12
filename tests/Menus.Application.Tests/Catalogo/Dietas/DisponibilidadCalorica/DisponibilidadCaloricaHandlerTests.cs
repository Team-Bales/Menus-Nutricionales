using Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;
using Menus.Application.Comun;
using Menus.Application.Tests.Catalogo;
using Menus.Application.Tests.Catalogo.Dietas;
using Menus.Application.Tests.Comun;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Catalogo.Dietas.DisponibilidadCalorica;

/// <summary>Pruebas del manejador de disponibilidad calórica (#101): cache-aside contra Redis simulado.</summary>
public sealed class DisponibilidadCaloricaHandlerTests
{
    private readonly BancoDeDietasFalso _banco = new();
    private readonly BancoFalso _items = new();
    private readonly CacheFalsa _cache = new();

    private DisponibilidadCaloricaHandler Handler() => new(_banco.DietasRepo, _items.ItemsRepo, _banco.MenusRepo, _cache);

    /// <summary>Alimento del banco, en un grupo y nivel dados, que no figura en ningún menú.</summary>
    private Guid NuevoItem(Guid grupoId, Guid nivelId)
    {
        var alimento = _items.Agregar("Item", nivel: nivelId, grupo: grupoId);
        return alimento.Id;
    }

    [Fact]
    public async Task Calcula_contra_los_repositorios_y_guarda_en_cache_cuando_no_esta_cacheado()
    {
        var permitido = BancoDeDietasFalso.GrupoFrutas;
        var prohibido = BancoDeDietasFalso.GrupoCereales;
        var dieta = _banco.Agregar("Dieta renal", grupoRequerido: permitido);
        dieta.AgregarGrupo(prohibido, TipoCobertura.Restringido);

        NuevoItem(permitido, NivelCalorico.Bajo.Id);
        NuevoItem(permitido, NivelCalorico.Bajo.Id);
        var itemProhibido = NuevoItem(prohibido, NivelCalorico.Alto.Id);
        var itemDelMenu = NuevoItem(permitido, NivelCalorico.Medio.Id);
        _banco.RegistrarNivelDeItem(itemDelMenu, NivelCalorico.Medio.Id);

        var creador = Guid.NewGuid();
        var menu = CrearMenuPendiente(dieta.Id, creador);
        menu.AgregarItem(itemDelMenu);
        menu.Aprobar(Guid.NewGuid(), menu.FechaCreacion.AddHours(1));

        var resultado = await Handler().HandleAsync(new DisponibilidadCaloricaQuery(dieta.Id), CancellationToken.None);

        Assert.Equal(dieta.Id, resultado.DietaId);
        Assert.Equal(3, resultado.PorNivel.Count);

        var bajo = resultado.PorNivel.Single(p => p.NivelCaloricoId == NivelCalorico.Bajo.Id);
        Assert.Equal(2, bajo.ItemsDelBanco);
        Assert.Equal(0, bajo.ItemsEnMenusFinales);

        var medio = resultado.PorNivel.Single(p => p.NivelCaloricoId == NivelCalorico.Medio.Id);
        Assert.Equal(1, medio.ItemsDelBanco);
        Assert.Equal(1, medio.ItemsEnMenusFinales);

        var alto = resultado.PorNivel.Single(p => p.NivelCaloricoId == NivelCalorico.Alto.Id);
        Assert.Equal(0, alto.ItemsDelBanco);

        _ = itemProhibido;
        Assert.Equal(1, _cache.Escrituras);
    }

    [Fact]
    public async Task Devuelve_el_valor_cacheado_sin_recalcularlo()
    {
        var dieta = _banco.Agregar("Dieta renal");
        var cacheado = new DisponibilidadCaloricaDto(dieta.Id, [new(NivelCalorico.Bajo.Id, "Bajo", 999, 999)]);
        await _cache.GuardarAsync(CacheDeDisponibilidadCalorica.Clave(dieta.Id), cacheado, TimeSpan.FromHours(1));

        var resultado = await Handler().HandleAsync(new DisponibilidadCaloricaQuery(dieta.Id), CancellationToken.None);

        Assert.Equal(999, resultado.PorNivel.Single().ItemsDelBanco);
    }

    [Fact]
    public async Task Si_la_cache_no_responde_calcula_igual_contra_los_repositorios()
    {
        var dieta = _banco.Agregar("Dieta renal");
        NuevoItem(BancoDeDietasFalso.GrupoFrutas, NivelCalorico.Bajo.Id);
        _cache.Disponible = false;

        var resultado = await Handler().HandleAsync(new DisponibilidadCaloricaQuery(dieta.Id), CancellationToken.None);

        var bajo = resultado.PorNivel.Single(p => p.NivelCaloricoId == NivelCalorico.Bajo.Id);
        Assert.Equal(1, bajo.ItemsDelBanco);
    }

    [Fact]
    public async Task Una_dieta_que_no_existe_es_404()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Handler().HandleAsync(new DisponibilidadCaloricaQuery(id), CancellationToken.None));

        Assert.Equal("dieta", error.Recurso);
        Assert.Equal(id, error.Id);
    }

    [Fact]
    public async Task Los_tres_niveles_aparecen_aunque_no_tengan_items()
    {
        var dieta = _banco.Agregar("Dieta renal");

        var resultado = await Handler().HandleAsync(new DisponibilidadCaloricaQuery(dieta.Id), CancellationToken.None);

        Assert.Equal(
            [NivelCalorico.Bajo.Id, NivelCalorico.Medio.Id, NivelCalorico.Alto.Id],
            resultado.PorNivel.Select(p => p.NivelCaloricoId));
        Assert.All(resultado.PorNivel, p => Assert.Equal(0, p.ItemsDelBanco));
    }

    [Fact]
    public void Los_DTO_tienen_igualdad_estructural()
    {
        var dietaId = Guid.NewGuid();
        var porNivel = new DisponibilidadPorNivelDto(NivelCalorico.Bajo.Id, "Bajo", 3, 1);
        IReadOnlyList<DisponibilidadPorNivelDto> lista = [porNivel];

        Assert.Equal(new DisponibilidadCaloricaDto(dietaId, lista), new DisponibilidadCaloricaDto(dietaId, lista));
        Assert.Equal(porNivel, porNivel with { });
    }

    private Menus.Domain.Entidades.Menu CrearMenuPendiente(Guid dietaId, Guid creadorId)
    {
        var menu = new Menus.Domain.Entidades.Menu(
            Guid.NewGuid(), dietaId, creadorId, DateTime.UtcNow.AddDays(-1), 1, true,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)], [BancoDeDietasFalso.GrupoCereales]);
        _banco.Menus.Add(menu);
        return menu;
    }
}
