using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de <see cref="CatalogoPlatos"/>: filtros, paginación, navegación y borrado (issue #65).</summary>
public sealed class CatalogoPlatosTests : BunitContext
{
    [Fact]
    public void Filtrar_pide_la_pagina_con_los_filtros_elegidos()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoPlatos>();
        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));

        cut.Find("#nivel").Change(CatalogoApiFalsa.Nivel.ToString());
        cut.Find("button.btn-outline-primary.flex-grow-1").Click();

        cut.WaitForAssertion(() =>
        {
            var (_, _, nivel, page, _) = api.UltimaConsultaPlatos!.Value;
            Assert.Equal(CatalogoApiFalsa.Nivel, nivel);
            Assert.Equal(1, page);
        });
    }

    [Fact]
    public void Limpiar_filtros_los_resetea_y_recarga_la_primera_pagina()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoPlatos>();
        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));

        cut.Find("#grupo").Change(CatalogoApiFalsa.Grupo.ToString());
        cut.Find(".card.shadow-sm.mb-4 button.btn-outline-secondary").Click();

        cut.WaitForAssertion(() =>
        {
            var (grupo, tipo, nivel, page, _) = api.UltimaConsultaPlatos!.Value;
            Assert.Null(grupo);
            Assert.Null(tipo);
            Assert.Null(nivel);
            Assert.Equal(1, page);
        });
    }

    [Fact]
    public void Paginacion_anterior_y_siguiente_cambian_de_pagina()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoPlatos>();
        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Siguiente").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, api.UltimaConsultaPlatos!.Value.Page));

        cut.FindAll("button").Single(b => b.TextContent == "Anterior").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, api.UltimaConsultaPlatos!.Value.Page));
    }

    [Fact]
    public void Un_error_al_cargar_los_nomencladores_no_impide_ver_la_pagina_de_platos()
    {
        var api = new CatalogoApiFalsa { ErrorAlCargarNomencladores = ErrorDePrueba.Conflicto("No se pudieron cargar los nomencladores.") };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoPlatos>();

        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));
    }

    [Fact]
    public void Nuevo_detalle_y_editar_navegan_a_las_rutas_correctas()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        var cut = Render<CatalogoPlatos>();
        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));

        cut.Find("button.btn-primary").Click();
        Assert.EndsWith("/catalogo/platos/nuevo", nav.Uri);

        cut.Find("button.btn-outline-secondary.me-1").Click();
        Assert.EndsWith($"/catalogo/platos/{CatalogoApiFalsa.PlatoId}", nav.Uri);

        cut.Find("button.btn-outline-primary.me-1").Click();
        Assert.EndsWith($"/catalogo/platos/{CatalogoApiFalsa.PlatoId}/editar", nav.Uri);
    }

    [Fact]
    public void Eliminar_quita_el_plato_y_recarga_la_pagina_actual()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoPlatos>();
        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));

        cut.Find("button.btn-outline-danger").Click();

        cut.WaitForAssertion(() => Assert.Equal(CatalogoApiFalsa.PlatoId, api.UltimoEliminado));
    }

    [Fact]
    public void Muestra_el_error_de_la_api_si_falla_al_eliminar()
    {
        var api = new CatalogoApiFalsa { ErrorAlEliminar = ErrorDePrueba.Conflicto("El plato está asignado a un menú.") };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoPlatos>();
        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));

        cut.Find("button.btn-outline-danger").Click();

        cut.WaitForAssertion(() => Assert.Contains("El plato está asignado a un menú.", cut.Markup));
    }

    [Fact]
    public void Muestra_el_error_de_la_api_si_falla_al_cargar_los_platos()
    {
        var api = new CatalogoApiFalsa { ErrorAlCargarPlato = ErrorDePrueba.Conflicto("No se pudieron cargar los platos.") };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoPlatos>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudieron cargar los platos.", cut.Markup));
    }

    [Fact]
    public void Sin_resultados_muestra_la_fila_vacia()
    {
        var api = new CatalogoApiFalsa { Platos = [] };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoPlatos>();

        cut.WaitForAssertion(() => Assert.Contains("No hay platos con esos filtros.", cut.Markup));
    }
}
