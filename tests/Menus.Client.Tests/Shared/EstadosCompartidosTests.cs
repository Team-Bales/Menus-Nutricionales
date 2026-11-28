using Bunit;
using Menus.Client.Shared;

namespace Menus.Client.Tests.Shared;

/// <summary>Pruebas de los componentes compartidos de carga, estado vacío (issue #346) y aviso de error (#349).</summary>
public sealed class EstadosCompartidosTests : BunitContext
{
    [Fact]
    public void EstadoVacio_muestra_el_detalle_cuando_se_proporciona()
    {
        var cut = Render<EstadoVacio>(ps => ps
            .Add(p => p.Texto, "No hay resultados")
            .Add(p => p.Detalle, "Prueba con otros filtros"));

        Assert.Contains("No hay resultados", cut.Markup);
        Assert.Contains("Prueba con otros filtros", cut.Markup);
    }

    [Fact]
    public void EstadoVacio_omite_el_detalle_cuando_no_se_proporciona()
    {
        var cut = Render<EstadoVacio>(ps => ps.Add(p => p.Texto, "No hay resultados"));

        Assert.Contains("No hay resultados", cut.Markup);
        Assert.DoesNotContain("Prueba con otros filtros", cut.Markup);
    }

    [Fact]
    public void EstadoDeCarga_muestra_el_texto_y_el_indicador_de_carga()
    {
        var cut = Render<EstadoDeCarga>(ps => ps.Add(p => p.Texto, "Cargando datos…"));

        Assert.Contains("Cargando datos…", cut.Markup);
        Assert.Contains("loading-state", cut.Markup);
        Assert.Contains("spinner", cut.Markup);
    }

    [Fact]
    public void AvisoDeError_muestra_el_mensaje_como_alerta()
    {
        var cut = Render<AvisoDeError>(ps => ps.Add(p => p.Mensaje, "No se pudo cargar."));

        Assert.Equal("No se pudo cargar.", cut.Find("[role=alert]").TextContent.Trim());
        Assert.Contains("alert-danger", cut.Find("[role=alert]").ClassName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void AvisoDeError_sin_mensaje_no_pinta_nada(string? mensaje)
    {
        var cut = Render<AvisoDeError>(ps => ps.Add(p => p.Mensaje, mensaje));

        Assert.Empty(cut.FindAll("[role=alert]"));
    }
}
