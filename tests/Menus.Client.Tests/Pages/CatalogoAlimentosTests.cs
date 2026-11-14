using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Catalogo;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de <see cref="CatalogoAlimentos"/>: filtros, paginación y navegación (issue #65).</summary>
public sealed class CatalogoAlimentosTests : BunitContext
{
    [Fact]
    public void Filtrar_pide_la_pagina_con_los_filtros_elegidos()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoAlimentos>();
        cut.WaitForAssertion(() => Assert.Contains("Avena", cut.Markup));

        cut.Find("#grupo").Change(CatalogoApiFalsa.Grupo.ToString());
        cut.Find("#tipo").Change(CatalogoApiFalsa.Tipo.ToString());
        cut.Find("button.btn-outline-primary.flex-grow-1").Click();

        cut.WaitForAssertion(() =>
        {
            var (grupo, tipo, _, page, _) = api.UltimaConsultaAlimentos!.Value;
            Assert.Equal(CatalogoApiFalsa.Grupo, grupo);
            Assert.Equal(CatalogoApiFalsa.Tipo, tipo);
            Assert.Equal(1, page);
        });
    }

    [Fact]
    public void Limpiar_filtros_los_resetea_y_recarga_la_primera_pagina()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoAlimentos>();
        cut.WaitForAssertion(() => Assert.Contains("Avena", cut.Markup));

        cut.Find("#grupo").Change(CatalogoApiFalsa.Grupo.ToString());
        cut.Find("button[title='Limpiar filtros']").Click();

        cut.WaitForAssertion(() =>
        {
            var (grupo, tipo, nivel, page, _) = api.UltimaConsultaAlimentos!.Value;
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

        var cut = Render<CatalogoAlimentos>();
        cut.WaitForAssertion(() => Assert.Contains("Avena", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Siguiente").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, api.UltimaConsultaAlimentos!.Value.Page));

        cut.FindAll("button").Single(b => b.TextContent == "Anterior").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, api.UltimaConsultaAlimentos!.Value.Page));
    }

    [Fact]
    public void Eliminar_quita_el_alimento_y_recarga_la_pagina_actual()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoAlimentos>();
        cut.WaitForAssertion(() => Assert.Contains("Avena", cut.Markup));

        cut.Find("button.btn-outline-danger").Click();

        cut.WaitForAssertion(() => Assert.Equal(CatalogoApiFalsa.AlimentoId, api.UltimoEliminado));
    }

    [Fact]
    public void Nuevo_y_editar_navegan_a_las_rutas_correctas()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        var cut = Render<CatalogoAlimentos>();
        cut.WaitForAssertion(() => Assert.Contains("Avena", cut.Markup));

        cut.Find("button.btn-primary").Click();
        Assert.EndsWith("/catalogo/alimentos/nuevo", nav.Uri);

        cut.Find("button.btn-outline-primary.me-2").Click();
        Assert.EndsWith($"/catalogo/alimentos/{CatalogoApiFalsa.AlimentoId}/editar", nav.Uri);
    }

    [Fact]
    public void Un_error_al_cargar_los_nomencladores_no_impide_ver_la_pagina_de_alimentos()
    {
        // CargarPagina corre después y resetea _error, así que la página sigue siendo usable aunque
        // los selectores de filtro queden vacíos.
        var api = new CatalogoApiFalsa { ErrorAlCargarNomencladores = ErrorDePrueba.Conflicto("No se pudieron cargar los nomencladores.") };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoAlimentos>();

        cut.WaitForAssertion(() => Assert.Contains("Avena", cut.Markup));
        Assert.DoesNotContain("No se pudieron cargar los nomencladores.", cut.Markup);
    }

    [Fact]
    public void Muestra_el_error_de_la_api_si_falla_al_cargar_los_alimentos()
    {
        var api = new CatalogoApiFalsa { ErrorAlCargarAlimento = ErrorDePrueba.Conflicto("No se pudieron cargar los alimentos.") };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoAlimentos>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudieron cargar los alimentos.", cut.Markup));
    }

    [Fact]
    public void Sin_resultados_muestra_la_fila_vacia()
    {
        var api = new CatalogoApiFalsa { Alimentos = [] };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoAlimentos>();

        cut.WaitForAssertion(() => Assert.Contains("No hay alimentos con esos filtros.", cut.Markup));
    }
}
