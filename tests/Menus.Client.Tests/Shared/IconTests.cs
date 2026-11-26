using Bunit;
using Menus.Client.Shared;

namespace Menus.Client.Tests.Shared;

/// <summary>Pruebas del componente de iconos SVG en línea (issue #347).</summary>
public sealed class IconTests : BunitContext
{
    [Theory]
    [InlineData("home")]
    [InlineData("basket")]
    [InlineData("utensils")]
    [InlineData("clipboard")]
    [InlineData("settings")]
    [InlineData("users")]
    [InlineData("hospital")]
    [InlineData("user")]
    [InlineData("logout")]
    [InlineData("chevron-right")]
    [InlineData("arrow-left")]
    [InlineData("arrow-up-right")]
    [InlineData("plus")]
    [InlineData("search")]
    [InlineData("bell")]
    [InlineData("check")]
    [InlineData("info")]
    [InlineData("alert")]
    [InlineData("leaf")]
    [InlineData("sparkles")]
    [InlineData("chart")]
    [InlineData("shield")]
    [InlineData("trash")]
    [InlineData("pencil")]
    [InlineData("eye")]
    [InlineData("filter")]
    [InlineData("calendar")]
    [InlineData("clock")]
    [InlineData("heart")]
    [InlineData("menu")]
    [InlineData("x")]
    [InlineData("folder")]
    [InlineData("activity")]
    [InlineData("desconocido")]
    public void Dibuja_un_trazo_con_el_nombre_pedido(string nombre)
    {
        var cut = Render<Icon>(ps => ps.Add(p => p.Name, nombre));

        var svg = cut.Find("svg");
        Assert.Contains("icon", svg.GetAttribute("class") ?? string.Empty);
        Assert.NotEmpty(svg.Children);
    }

    [Fact]
    public void Acepta_clases_adicionales_y_es_decorativo()
    {
        var cut = Render<Icon>(ps => ps
            .Add(p => p.Name, "home")
            .Add(p => p.CssClass, "mi-clase"));

        var svg = cut.Find("svg");
        Assert.Contains("mi-clase", svg.GetAttribute("class") ?? string.Empty);
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
    }
}
