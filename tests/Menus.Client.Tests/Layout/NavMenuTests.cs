using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Layout;
using Menus.Client.Services;
using Menus.Client.Tests.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Layout;

/// <summary>Pruebas de <see cref="NavMenu"/>: enlaces del catálogo y colapso del menú.</summary>
public sealed class NavMenuTests : BunitContext
{
    private void AutenticarComoAdmin()
    {
        Services.AddSingleton<ISesionAutenticada>(new SesionAutenticadaFalsa());
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles("Admin");
    }

    [Fact]
    public void Muestra_los_enlaces_al_catalogo()
    {
        AutenticarComoAdmin();
        var cut = Render<NavMenu>();

        Assert.Contains("catalogo/alimentos", cut.Markup);
        Assert.Contains("catalogo/platos", cut.Markup);
        Assert.Contains("catalogo/dietas", cut.Markup);
        Assert.Contains("admin/nomencladores", cut.Markup);
    }

    [Fact]
    public void Un_nutricionista_ve_el_catalogo_pero_no_la_administracion()
    {
        Services.AddSingleton<ISesionAutenticada>(new SesionAutenticadaFalsa());
        AddAuthorization().SetAuthorized("nutri@centro.test").SetRoles(RolesDeUsuario.Nutricionista);

        var cut = Render<NavMenu>();

        Assert.Contains("catalogo/alimentos", cut.Markup);
        Assert.Contains("catalogo/dietas", cut.Markup);
        Assert.DoesNotContain("admin/nomencladores", cut.Markup);
    }

    [Fact]
    public void Un_paciente_solo_ve_inicio_perfil_y_cerrar_sesion()
    {
        Services.AddSingleton<ISesionAutenticada>(new SesionAutenticadaFalsa());
        AddAuthorization().SetAuthorized("paciente@centro.test").SetRoles(RolesDeUsuario.Paciente);

        var cut = Render<NavMenu>();

        Assert.DoesNotContain("catalogo/", cut.Markup);
        Assert.DoesNotContain("admin/nomencladores", cut.Markup);
        Assert.Contains("perfil", cut.Markup);
        Assert.Contains(cut.FindAll("button"), b => b.TextContent == "Cerrar sesión");
    }

    [Fact]
    public void Sin_sesion_solo_ofrece_iniciar_sesion()
    {
        Services.AddSingleton<ISesionAutenticada>(new SesionAutenticadaFalsa());
        AddAuthorization();

        var cut = Render<NavMenu>();

        Assert.Contains("Iniciar sesión", cut.Markup);
        Assert.DoesNotContain("catalogo/", cut.Markup);
        Assert.DoesNotContain("Cerrar sesión", cut.Markup);
    }

    [Fact]
    public void El_boton_de_navegacion_colapsa_y_expande_el_menu()
    {
        AutenticarComoAdmin();
        var cut = Render<NavMenu>();
        var navScrollable = cut.Find("div.nav-scrollable");
        Assert.Contains("collapse", navScrollable.ClassName);

        cut.Find("button.navbar-toggler").Click();
        Assert.DoesNotContain("collapse", cut.Find("div.nav-scrollable").ClassName);

        cut.Find("button.navbar-toggler").Click();
        Assert.Contains("collapse", cut.Find("div.nav-scrollable").ClassName);
    }

    [Fact]
    public void Cerrar_sesion_elimina_la_sesion_y_navega_al_login()
    {
        var sesion = new SesionAutenticadaFalsa();
        Services.AddSingleton<ISesionAutenticada>(sesion);
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles("Admin");
        var cut = Render<NavMenu>();

        cut.FindAll("button").Single(button => button.TextContent == "Cerrar sesión").Click();

        cut.WaitForAssertion(() => Assert.True(sesion.SeCerro));
        Assert.EndsWith("/login", Services.GetRequiredService<BunitNavigationManager>().Uri);
    }
}
