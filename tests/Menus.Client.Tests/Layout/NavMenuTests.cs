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
        Assert.Contains("admin/nutricionistas", cut.Markup);
        Assert.Contains("admin/salas", cut.Markup);
        Assert.Contains("admin/pacientes", cut.Markup);
    }

    [Fact]
    public void Un_nutricionista_ve_el_catalogo_pero_no_la_administracion()
    {
        Services.AddSingleton<ISesionAutenticada>(new SesionAutenticadaFalsa());
        AddAuthorization().SetAuthorized("nutri@centro.test").SetRoles(RolesDeUsuario.Nutricionista);

        var cut = Render<NavMenu>();

        Assert.Contains("catalogo/alimentos", cut.Markup);
        Assert.Contains("catalogo/dietas", cut.Markup);
        Assert.Contains("admin/pacientes", cut.Markup);
        Assert.DoesNotContain("admin/nomencladores", cut.Markup);
        Assert.DoesNotContain("admin/nutricionistas", cut.Markup);
        Assert.DoesNotContain("admin/salas", cut.Markup);
    }

    [Fact]
    public void Un_paciente_solo_ve_inicio_perfil_y_cerrar_sesion()
    {
        Services.AddSingleton<ISesionAutenticada>(new SesionAutenticadaFalsa());
        AddAuthorization().SetAuthorized("paciente@centro.test").SetRoles(RolesDeUsuario.Paciente);

        var cut = Render<NavMenu>();

        Assert.DoesNotContain("catalogo/", cut.Markup);
        Assert.DoesNotContain("admin/nomencladores", cut.Markup);
        Assert.DoesNotContain("admin/pacientes", cut.Markup);
        Assert.Contains("mi-perfil", cut.Markup);
        Assert.Contains("Cerrar sesión", cut.Markup);
        Assert.NotNull(cut.Find("button.sidebar-logout"));
        Assert.Equal("DIV", cut.Find(".sidebar-footer-role").TagName);
        Assert.Equal("DIV", cut.Find(".sidebar-footer-caption").TagName);
        Assert.DoesNotContain(">MU<", cut.Markup);
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

        var boton = cut.Find("button.navbar-toggler");
        Assert.Equal("Menú de navegación", boton.GetAttribute("aria-label"));
        Assert.Equal("false", boton.GetAttribute("aria-expanded"));

        boton.Click();
        Assert.DoesNotContain("collapse", cut.Find("div.nav-scrollable").ClassName);
        Assert.Equal("true", cut.Find("button.navbar-toggler").GetAttribute("aria-expanded"));

        cut.Find("button.navbar-toggler").Click();
        Assert.Contains("collapse", cut.Find("div.nav-scrollable").ClassName);
        Assert.Equal("false", cut.Find("button.navbar-toggler").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Cerrar_sesion_elimina_la_sesion_y_navega_al_login()
    {
        var sesion = new SesionAutenticadaFalsa();
        Services.AddSingleton<ISesionAutenticada>(sesion);
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles("Admin");
        var cut = Render<NavMenu>();

        cut.Find("button.sidebar-logout").Click();

        cut.WaitForAssertion(() => Assert.True(sesion.SeCerro));
        Assert.EndsWith("/login", Services.GetRequiredService<BunitNavigationManager>().Uri);
    }
}
