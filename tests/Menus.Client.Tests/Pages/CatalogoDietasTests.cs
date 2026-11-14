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
}
