using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Layout;
using Menus.Client.Services;
using Menus.Client.Tests.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Layout;

/// <summary>Pruebas de <see cref="MainLayout"/>: envuelve el contenido con el menú de navegación.</summary>
public sealed class MainLayoutTests : BunitContext
{
    [Fact]
    public void Muestra_el_menu_y_el_contenido_de_la_pagina()
    {
        Services.AddSingleton<ISesionAutenticada>(new SesionAutenticadaFalsa());
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles("Admin");
        RenderFragment body = builder => builder.AddMarkupContent(0, "<p>Contenido de la página</p>");

        var cut = Render<MainLayout>(ps => ps.Add(p => p.Body, body));

        Assert.Contains("Contenido de la página", cut.Markup);
        Assert.Contains("app-topbar", cut.Markup);
        Assert.Contains("Menús nutricionales", cut.Markup);
        Assert.DoesNotContain("Buscar", cut.Markup);
        Assert.DoesNotContain("Notificaciones", cut.Markup);
        Assert.DoesNotContain("Gestión nutricional", cut.Markup);
        Assert.DoesNotContain(">MU<", cut.Markup);
        Assert.NotNull(cut.Find(".topbar-user .avatar .icon"));
    }

    /// <summary>La barra superior debe mostrar la sección que corresponda a la ruta actual.</summary>
    [Theory]
    [InlineData("catalogo/alimentos", "Alimentos")]
    [InlineData("catalogo/platos", "Platos")]
    [InlineData("catalogo/dietas", "Dietas")]
    [InlineData("menus", "Menús")]
    [InlineData("admin/nomencladores", "Nomencladores")]
    [InlineData("admin/nutricionistas", "Nutricionistas")]
    [InlineData("admin/salas", "Salas")]
    [InlineData("perfil", "Mi perfil")]
    [InlineData("login", "Iniciar sesión")]
    public void Muestra_el_titulo_de_la_seccion_actual(string ruta, string titulo)
    {
        Services.AddSingleton<ISesionAutenticada>(new SesionAutenticadaFalsa());
        AddAuthorization().SetAuthorized("admin@centro.test").SetRoles("Admin");
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo($"http://localhost/{ruta}");
        RenderFragment body = builder => builder.AddMarkupContent(0, "<p>Contenido de la página</p>");

        var cut = Render<MainLayout>(ps => ps.Add(p => p.Body, body));

        Assert.Contains(titulo, cut.Find(".topbar-title").TextContent);
    }
}
