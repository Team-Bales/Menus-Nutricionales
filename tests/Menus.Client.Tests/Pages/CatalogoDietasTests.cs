using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas del listado de dietas del issue #66.</summary>
public sealed class CatalogoDietasTests : BunitContext
{
    [Fact]
    public void Muestra_los_datos_de_la_dieta_y_sus_conteos()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<CatalogoDietas>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Dieta basal", cut.Markup);
            Assert.Contains("Atención primaria", cut.Markup);
            Assert.Contains(">1<", cut.Markup);
        });
    }

    [Fact]
    public void Filtrar_envia_el_programa_elegido_y_vuelve_a_la_primera_pagina()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoDietas>();
        cut.WaitForAssertion(() => Assert.Contains("Dieta basal", cut.Markup));

        cut.Find("#programa").Change(CatalogoApiFalsa.Programa.ToString());
        cut.FindAll("button").Single(button => button.TextContent == "Filtrar").Click();

        cut.WaitForAssertion(() =>
        {
            var (programa, page, size) = api.UltimaConsultaDietas!.Value;
            Assert.Equal(CatalogoApiFalsa.Programa, programa);
            Assert.Equal(1, page);
            Assert.Equal(10, size);
        });
    }

    [Fact]
    public void Ver_detalle_navega_a_la_dieta_elegida()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        var cut = Render<CatalogoDietas>();
        cut.WaitForAssertion(() => Assert.Contains("Dieta basal", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent == "Ver detalle").Click();

        Assert.EndsWith($"/catalogo/dietas/{CatalogoApiFalsa.DietaId}", nav.Uri);
    }

    [Fact]
    public void Limpiar_quita_el_filtro_y_recarga_la_primera_pagina()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoDietas>();
        cut.WaitForAssertion(() => Assert.Contains("Dieta basal", cut.Markup));

        cut.Find("#programa").Change(CatalogoApiFalsa.Programa.ToString());
        cut.FindAll("button").Single(button => button.TextContent == "Limpiar").Click();

        cut.WaitForAssertion(() =>
        {
            var (programa, page, _) = api.UltimaConsultaDietas!.Value;
            Assert.Null(programa);
            Assert.Equal(1, page);
        });
    }

    [Fact]
    public void Paginacion_avanza_y_retrocede_entre_paginas()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoDietas>();
        cut.WaitForAssertion(() => Assert.Contains("Dieta basal", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent == "Siguiente").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, api.UltimaConsultaDietas!.Value.Page));

        cut.FindAll("button").Single(button => button.TextContent == "Anterior").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, api.UltimaConsultaDietas!.Value.Page));
    }

    [Fact]
    public void Sin_dietas_muestra_el_estado_vacio()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa { Dietas = [] });

        var cut = Render<CatalogoDietas>();

        cut.WaitForAssertion(() => Assert.Contains("No hay dietas con esos filtros.", cut.Markup));
    }

    [Fact]
    public void Error_al_cargar_las_dietas_se_muestra_de_forma_legible()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa
        {
            ErrorAlCargarDieta = ErrorDePrueba.Conflicto("No se pudieron cargar las dietas.")
        });

        var cut = Render<CatalogoDietas>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudieron cargar las dietas.", cut.Markup));
    }

    [Fact]
    public void Error_al_cargar_programas_se_muestra_y_no_intenta_cargar_dietas()
    {
        var api = new CatalogoApiFalsa
        {
            ErrorAlCargarNomencladores = ErrorDePrueba.Conflicto("No se pudieron cargar los programas.")
        };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<CatalogoDietas>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudieron cargar los programas.", cut.Markup));
        Assert.Null(api.UltimaConsultaDietas);
    }
}
