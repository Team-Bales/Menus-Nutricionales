using System.Net;
using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>
/// Pruebas de la portada (<see cref="Home"/>) y de que no queden páginas de la plantilla de Blazor (#337).
/// </summary>
public sealed class HomeTests : BunitContext
{
    private static readonly string[] EnlacesDelCatalogo = ["catalogo/alimentos", "catalogo/platos", "catalogo/dietas"];

    private static readonly string[] EnlacesDeAdministracion = ["admin/nomencladores", "admin/nutricionistas", "admin/salas"];

    private IRenderedComponent<Home> RenderComo(string correo, string rol)
    {
        AddAuthorization().SetAuthorized(correo).SetRoles(rol);
        Servicios(new CatalogoApiFalsa(), new AdministracionApiFalsa());
        return Render<Home>();
    }

    private void Servicios(ICatalogoApi catalogo, IAdministracionApi administracion)
    {
        Services.AddSingleton(catalogo);
        Services.AddSingleton(administracion);
    }

    private static IReadOnlyList<string> Enlaces(IRenderedComponent<Home> cut) =>
        cut.FindAll("a").Select(a => a.GetAttribute("href") ?? string.Empty).ToList();

    [Fact]
    public void Un_admin_ve_el_catalogo_la_administracion_y_el_perfil()
    {
        var enlaces = Enlaces(RenderComo("admin@centro.test", RolesDeUsuario.Admin));

        Assert.All(EnlacesDelCatalogo, e => Assert.Contains(e, enlaces));
        Assert.All(EnlacesDeAdministracion, e => Assert.Contains(e, enlaces));
        Assert.Contains("perfil", enlaces);
    }

    [Fact]
    public void Un_nutricionista_ve_el_catalogo_y_el_perfil_pero_no_la_administracion()
    {
        var enlaces = Enlaces(RenderComo("nutri@centro.test", RolesDeUsuario.Nutricionista));

        Assert.All(EnlacesDelCatalogo, e => Assert.Contains(e, enlaces));
        Assert.All(EnlacesDeAdministracion, e => Assert.DoesNotContain(e, enlaces));
        Assert.Contains("perfil", enlaces);
    }

    [Fact]
    public void Un_paciente_solo_ve_el_perfil()
    {
        var enlaces = Enlaces(RenderComo("paciente@centro.test", RolesDeUsuario.Paciente));

        Assert.Equal(new[] { "perfil" }, enlaces);
    }

    [Fact]
    public void Esta_en_espanol_y_sin_el_texto_de_la_plantilla()
    {
        var cut = RenderComo("admin@centro.test", RolesDeUsuario.Admin);

        Assert.Equal("Menús nutricionales", cut.Find("h1").TextContent);
        Assert.DoesNotContain("Hello, world", cut.Markup);
        Assert.DoesNotContain("Welcome to your new app", cut.Markup);
    }

    [Fact]
    public void No_quedan_las_rutas_de_ejemplo_de_la_plantilla()
    {
        var rutas = typeof(Home).Assembly.GetTypes()
            .SelectMany(t => t.GetCustomAttributes<RouteAttribute>())
            .Select(r => r.Template)
            .ToList();

        Assert.Contains("/", rutas);
        Assert.DoesNotContain("/counter", rutas);
        Assert.DoesNotContain("/weather", rutas);
    }

    [Fact]
    public void La_pagina_de_ruta_inexistente_esta_en_espanol()
    {
        var cut = Render<NotFound>();

        Assert.Equal("Página no encontrada", cut.Find("h1").TextContent);
        Assert.DoesNotContain("Sorry", cut.Markup);
    }

    [Fact]
    public void Un_admin_ve_las_cinco_metricas_del_catalogo_y_la_administracion()
    {
        var cut = RenderComo("admin@centro.test", RolesDeUsuario.Admin);

        cut.WaitForAssertion(() => Assert.All(cut.FindAll(".stat-value"), valor => Assert.Equal("1", valor.TextContent.Trim())));
        Assert.Equal(5, cut.FindAll(".stat-card").Count);
    }

    [Fact]
    public void Un_nutricionista_ve_solo_las_tres_metricas_del_catalogo()
    {
        var cut = RenderComo("nutri@centro.test", RolesDeUsuario.Nutricionista);

        cut.WaitForAssertion(() => Assert.All(cut.FindAll(".stat-value"), valor => Assert.Equal("1", valor.TextContent.Trim())));
        Assert.Equal(3, cut.FindAll(".stat-card").Count);
    }

    [Fact]
    public void Un_paciente_no_ve_metricas_y_sigue_con_un_solo_enlace()
    {
        var cut = RenderComo("paciente@centro.test", RolesDeUsuario.Paciente);

        Assert.Empty(cut.FindAll(".stat-card"));
        Assert.Equal(new[] { "perfil" }, Enlaces(cut));
    }

    [Fact]
    public void Sin_dietas_no_se_muestra_la_seccion_de_dietas()
    {
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);
        Servicios(new CatalogoApiFalsa { Dietas = [] }, new AdministracionApiFalsa());

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll(".stat-card").Count));
        Assert.DoesNotContain("portada-dietas", cut.Markup);
    }

    [Fact]
    public void Las_primeras_dietas_se_muestran_como_tales_con_un_enlace_ver_todas()
    {
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);
        Servicios(new CatalogoApiFalsa(), new AdministracionApiFalsa());

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains("portada-dietas", cut.Markup));
        Assert.Contains("Dieta basal", cut.Markup);
        Assert.Contains("Ver todas", cut.Markup);
        Assert.DoesNotContain("Dietas recientes", cut.Markup);
    }

    [Fact]
    public void Solo_se_pide_una_pagina_de_dietas_para_el_total_y_la_lista()
    {
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);
        var catalogo = new CatalogoApiFalsa();
        Servicios(catalogo, new AdministracionApiFalsa());

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains("Ver todas", cut.Markup));
        Assert.Equal(1, catalogo.ConsultasDeDietas);
        Assert.Equal(5, catalogo.UltimaConsultaDietas!.Value.Size);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Un_fallo_de_conexion_muestra_el_aviso_y_conserva_las_cifras_que_llegaron(bool esSinConexion)
    {
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);
        Servicios(new CatalogoApiFalsa
        {
            ErrorDeConexion = esSinConexion ? new HttpRequestException() : new TaskCanceledException()
        }, new AdministracionApiFalsa());

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains(MensajesDeConexion.SinServidor, cut.Markup));
        cut.WaitForAssertion(() => Assert.Equal("1", cut.FindAll(".stat-value").ElementAt(4).TextContent.Trim()));
        var valores = cut.FindAll(".stat-value").Select(v => v.TextContent.Trim()).ToList();
        Assert.Equal("—", valores[0]);
        Assert.Equal("1", valores[1]);
        Assert.Equal("1", valores[2]);
        Assert.Equal("1", valores[4]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Un_fallo_de_conexion_en_la_administracion_muestra_el_aviso_y_conserva_las_salas(bool esSinConexion)
    {
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);
        Servicios(new CatalogoApiFalsa(), new AdministracionApiFalsa
        {
            ErrorDeConexion = esSinConexion ? new HttpRequestException() : new TaskCanceledException()
        });

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains(MensajesDeConexion.SinServidor, cut.Markup));
        cut.WaitForAssertion(() => Assert.Equal("—", cut.FindAll(".stat-value").ElementAt(4).TextContent.Trim()));
        Assert.Equal("1", cut.FindAll(".stat-value")[0].TextContent.Trim());
        Assert.Equal("1", cut.FindAll(".stat-value")[3].TextContent.Trim());
    }

    [Fact]
    public void Un_fallo_parcial_del_catalogo_conserva_las_demas_cifras()
    {
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);
        Servicios(new CatalogoApiFalsa
        {
            ErrorAlCargarAlimento = new ApiException(HttpStatusCode.InternalServerError, "No se pudieron cargar los alimentos.")
        }, new AdministracionApiFalsa());

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudieron cargar los alimentos.", cut.Markup));
        var valores = cut.FindAll(".stat-value").Select(v => v.TextContent.Trim()).ToList();
        Assert.Equal("—", valores[0]);
        Assert.Equal("1", valores[1]);
        Assert.Equal("1", valores[2]);
    }

    [Fact]
    public void Un_fallo_parcial_de_administracion_conserva_las_salas()
    {
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);
        Servicios(new CatalogoApiFalsa(), new AdministracionApiFalsa
        {
            ErrorAlListarNutricionistas = new ApiException(HttpStatusCode.InternalServerError, "No se pudieron cargar los nutricionistas.")
        });

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudieron cargar los nutricionistas.", cut.Markup));
        var valores = cut.FindAll(".stat-value").Select(v => v.TextContent.Trim()).ToList();
        Assert.Equal("—", valores[3]);
        Assert.Equal("1", valores[4]);
    }

    [Fact]
    public void Si_fallan_catalogo_y_administracion_se_conserva_el_primer_error()
    {
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);
        Servicios(new CatalogoApiFalsa
        {
            ErrorAlCargarAlimento = new ApiException(HttpStatusCode.InternalServerError, "Fallo del catálogo.")
        }, new AdministracionApiFalsa
        {
            Error = new ApiException(HttpStatusCode.InternalServerError, "Fallo de administración.")
        });

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains("Fallo del catálogo.", cut.Markup));
        Assert.DoesNotContain("Fallo de administración.", cut.Markup);
    }

    [Fact]
    public void Un_error_del_catalogo_muestra_un_aviso_y_no_rompe_la_portada()
    {
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);
        Servicios(new CatalogoApiFalsa
        {
            ErrorAlCargarAlimento = new ApiException(HttpStatusCode.InternalServerError, "No se pudo cargar el catálogo."),
        }, new AdministracionApiFalsa());

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudo cargar el catálogo.", cut.Markup));
        Assert.Contains("perfil", cut.Markup);
    }

    [Fact]
    public void Un_error_de_administracion_muestra_un_aviso_y_no_rompe_la_portada()
    {
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);
        Servicios(new CatalogoApiFalsa(), new AdministracionApiFalsa
        {
            Error = new ApiException(HttpStatusCode.InternalServerError, "No se pudo cargar la administración."),
        });

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudo cargar la administración.", cut.Markup));
        Assert.Contains("perfil", cut.Markup);
    }
}
