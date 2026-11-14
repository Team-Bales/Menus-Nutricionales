using Bunit;
using Menus.Client.Layout;
using Microsoft.AspNetCore.Components;

namespace Menus.Client.Tests.Layout;

/// <summary>Pruebas de <see cref="MainLayout"/>: envuelve el contenido con el menú de navegación.</summary>
public sealed class MainLayoutTests : BunitContext
{
    [Fact]
    public void Muestra_el_menu_y_el_contenido_de_la_pagina()
    {
        RenderFragment body = builder => builder.AddMarkupContent(0, "<p>Contenido de la página</p>");

        var cut = Render<MainLayout>(ps => ps.Add(p => p.Body, body));

        Assert.Contains("Contenido de la página", cut.Markup);
        Assert.Contains("Gestión del catálogo nutricional", cut.Markup);
        Assert.Contains("Menús nutricionales", cut.Markup);
    }
}
