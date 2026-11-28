using Bunit;
using Menus.Client.Shared;

namespace Menus.Client.Tests.Shared;

/// <summary>Pruebas de los componentes compartidos de carga y estado vacío (issue #346).</summary>
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
}
