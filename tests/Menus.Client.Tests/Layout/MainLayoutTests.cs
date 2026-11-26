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
    }
}
