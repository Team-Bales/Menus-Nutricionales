using Menus.Application.Comun;
using Menus.Application.GeneracionDeMenus.GenerarMenuAutomaticamente;
using Menus.Application.Tests.Catalogo;
using Menus.Application.Tests.Catalogo.Dietas;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.GeneracionDeMenus;

/// <summary>Pruebas del manejador de generación automática de menús (#98).</summary>
public sealed class GenerarMenuAutomaticamenteHandlerTests
{
    private readonly BancoDeDietasFalso _banco = new();
    private readonly BancoFalso _items = new();
    private readonly EstrategiaFalsa _estrategia = new();

    private GenerarMenuAutomaticamenteHandler Handler(Guid? actor = null) => new(
        _banco.DietasRepo, _items.ItemsRepo, _banco.MenusRepo, _banco.NutricionistasRepo,
        _banco.UnidadDeTrabajo, new ActorFalso(actor ?? Guid.Empty), _estrategia);

    private static GenerarMenuAutomaticamenteCommand NuevoComando(Guid dietaId) => new(
        dietaId,
        CantidadTotalAlimentos: 2,
        Distribucion: [(TipoPreparacion.Entrante.Id, 1m)],
        GruposNutricionalesIds: [BancoDeDietasFalso.GrupoCereales]);

    [Fact]
    public async Task Genera_el_menu_automatico_con_los_items_de_la_estrategia_y_confirma()
    {
        var dieta = _banco.Agregar("Dieta renal");
        var nutricionista = _banco.AgregarNutricionista(dieta.Id);
        var item1 = _items.Agregar("Arroz");
        var item2 = _items.Agregar("Pollo");
        _estrategia.Resultado = [item1.Id, item2.Id];

        var creado = await Handler(nutricionista.Id).HandleAsync(NuevoComando(dieta.Id), CancellationToken.None);

        var menu = Assert.Single(_banco.Menus);
        Assert.Equal(creado.Id, menu.Id);
        Assert.Equal(dieta.Id, menu.DietaId);
        Assert.Equal(nutricionista.Id, menu.NutricionistaCreadorId);
        Assert.True(menu.EsAutomatico);
        Assert.Equal(2, menu.CantidadTotalAlimentos);
        Assert.Equal([item1.Id, item2.Id], menu.Items.Select(i => i.ItemId));
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Una_dieta_que_no_existe_es_404()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Handler().HandleAsync(NuevoComando(id), CancellationToken.None));

        Assert.Equal("dieta", error.Recurso);
        Assert.Equal(id, error.Id);
    }

    [Fact]
    public async Task Un_nutricionista_no_autorizado_para_la_dieta_no_puede_generar_el_menu()
    {
        var dieta = _banco.Agregar("Dieta renal");
        var otraDieta = _banco.Agregar("Dieta blanda");
        var nutricionista = _banco.AgregarNutricionista(otraDieta.Id); // autorizado para otra, no para esta

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Handler(nutricionista.Id).HandleAsync(NuevoComando(dieta.Id), CancellationToken.None));

        Assert.Equal("RS-03", error.Regla);
        Assert.Empty(_banco.Menus);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Un_actor_que_no_es_un_nutricionista_existente_es_404()
    {
        var dieta = _banco.Agregar("Dieta renal");

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Handler(Guid.NewGuid()).HandleAsync(NuevoComando(dieta.Id), CancellationToken.None));

        Assert.Equal("nutricionista", error.Recurso);
    }

    [Fact]
    public async Task No_le_pasa_a_la_estrategia_items_de_grupos_prohibidos()
    {
        var dieta = _banco.Agregar("Dieta renal");
        dieta.AgregarGrupo(BancoDeDietasFalso.GrupoFrutas, TipoCobertura.Restringido);
        var nutricionista = _banco.AgregarNutricionista(dieta.Id);
        var compatible = _items.Agregar("Arroz", grupo: BancoDeDietasFalso.GrupoCereales);
        var prohibido = _items.Agregar("Manzana", grupo: BancoDeDietasFalso.GrupoFrutas);
        _estrategia.Resultado = [compatible.Id];

        await Handler(nutricionista.Id).HandleAsync(NuevoComando(dieta.Id), CancellationToken.None);

        Assert.Contains(_estrategia.ItemsRecibidos!, i => i.Id == compatible.Id);
        Assert.DoesNotContain(_estrategia.ItemsRecibidos!, i => i.Id == prohibido.Id);
    }
}
