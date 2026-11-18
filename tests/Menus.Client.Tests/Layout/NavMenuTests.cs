using Bunit;
using Menus.Client.Layout;

namespace Menus.Client.Tests.Layout;

/// <summary>Pruebas de <see cref="NavMenu"/>: enlaces del catálogo y colapso del menú.</summary>
public sealed class NavMenuTests : BunitContext
{
    [Fact]
    public void Muestra_los_enlaces_al_catalogo()
    {
        var cut = Render<NavMenu>();

        Assert.Contains("catalogo/alimentos", cut.Markup);
        Assert.Contains("catalogo/platos", cut.Markup);
        Assert.Contains("catalogo/dietas", cut.Markup);
        Assert.Contains("admin/nomencladores", cut.Markup);
    }

    [Fact]
    public void El_boton_de_navegacion_colapsa_y_expande_el_menu()
    {
        var cut = Render<NavMenu>();
        var navScrollable = cut.Find("div.nav-scrollable");
        Assert.Contains("collapse", navScrollable.ClassName);

        cut.Find("button.navbar-toggler").Click();
        Assert.DoesNotContain("collapse", cut.Find("div.nav-scrollable").ClassName);

        cut.Find("button.navbar-toggler").Click();
        Assert.Contains("collapse", cut.Find("div.nav-scrollable").ClassName);
    }
}
