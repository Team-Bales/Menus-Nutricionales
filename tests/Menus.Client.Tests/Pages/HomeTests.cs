using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.AspNetCore.Components;

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
        return Render<Home>();
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
}
