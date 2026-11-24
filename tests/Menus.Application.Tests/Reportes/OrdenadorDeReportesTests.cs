using Menus.Application.Reportes;

namespace Menus.Application.Tests.Reportes;

/// <summary>Pruebas de la utilidad <see cref="OrdenadorDeReportes"/> para ordenamiento en memoria.</summary>
public sealed class OrdenadorDeReportesTests
{
    private sealed record Item(string Nombre, int Valor, DateTime Fecha);

    private static readonly IReadOnlyList<Item> DatosBase = [
        new("Charlie", 3, new DateTime(2024, 3, 1)),
        new("Alpha", 1, new DateTime(2024, 1, 1)),
        new("Beta", 2, new DateTime(2024, 2, 1))
    ];

    // ---- Sin orden ----

    [Fact]
    public void SinOrden_ConservaOrdenOriginal()
    {
        var resultado = DatosBase.AplicarOrden(null);
        Assert.Equal(DatosBase, resultado);
    }

    [Fact]
    public void OrdenNull_ConservaOrdenOriginal()
    {
        var resultado = DatosBase.AplicarOrden(new OrdenDeReporte(null, "asc"));
        Assert.Equal(DatosBase, resultado);
    }

    [Fact]
    public void OrdenSinColumna_ConservaOrdenOriginal()
    {
        var resultado = DatosBase.AplicarOrden(new OrdenDeReporte("", "asc"));
        Assert.Equal(DatosBase, resultado);
    }

    [Fact]
    public void OrdenSoloDireccionSinColumna_ConservaOrdenOriginal()
    {
        var resultado = DatosBase.AplicarOrden(new OrdenDeReporte("   ", "desc"));
        Assert.Equal(DatosBase, resultado);
    }

    // ---- Ascendente ----

    [Fact]
    public void AscPorString_OrdenaAlfabeticamente()
    {
        var resultado = DatosBase.AplicarOrden(new OrdenDeReporte("Nombre", "asc"));
        Assert.Equal("Alpha", resultado[0].Nombre);
        Assert.Equal("Beta", resultado[1].Nombre);
        Assert.Equal("Charlie", resultado[2].Nombre);
    }

    [Fact]
    public void AscPorInt_OrdenaNumericamente()
    {
        var resultado = DatosBase.AplicarOrden(new OrdenDeReporte("Valor", "asc"));
        Assert.Equal(1, resultado[0].Valor);
        Assert.Equal(2, resultado[1].Valor);
        Assert.Equal(3, resultado[2].Valor);
    }

    [Fact]
    public void AscPorDateTime_OrdenaCronologicamente()
    {
        var resultado = DatosBase.AplicarOrden(new OrdenDeReporte("Fecha", "asc"));
        Assert.Equal(new DateTime(2024, 1, 1), resultado[0].Fecha);
        Assert.Equal(new DateTime(2024, 2, 1), resultado[1].Fecha);
        Assert.Equal(new DateTime(2024, 3, 1), resultado[2].Fecha);
    }

    // ---- Descendente ----

    [Fact]
    public void DescPorString_OrdenaInverso()
    {
        var resultado = DatosBase.AplicarOrden(new OrdenDeReporte("Nombre", "desc"));
        Assert.Equal("Charlie", resultado[0].Nombre);
        Assert.Equal("Beta", resultado[1].Nombre);
        Assert.Equal("Alpha", resultado[2].Nombre);
    }

    [Fact]
    public void DescPorInt_OrdenaInverso()
    {
        var resultado = DatosBase.AplicarOrden(new OrdenDeReporte("Valor", "desc"));
        Assert.Equal(3, resultado[0].Valor);
        Assert.Equal(2, resultado[1].Valor);
        Assert.Equal(1, resultado[2].Valor);
    }

    // ---- Case-insensitive ----

    [Fact]
    public void ColumnaCaseInsensitive_Funciona()
    {
        var resultado = DatosBase.AplicarOrden(new OrdenDeReporte("nOmBrE", "asc"));
        Assert.Equal("Alpha", resultado[0].Nombre);
    }

    [Fact]
    public void DireccionCaseInsensitive_Funciona()
    {
        var resultado = DatosBase.AplicarOrden(new OrdenDeReporte("Nombre", "DESC"));
        Assert.Equal("Charlie", resultado[0].Nombre);
    }

    // ---- Errores ----

    [Fact]
    public void ColumnaInexistente_LanzaArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => DatosBase.AplicarOrden(new OrdenDeReporte("NoExiste", "asc")));
        Assert.Contains("NoExiste", ex.Message);
        Assert.Contains("Item", ex.Message);
    }

    // ---- Estabilidad (desempate por orden original) ----

    [Fact]
    public void OrdenEstable_ValoresIgualesConservanOrdenOriginal()
    {
        var datosConEmpate = new List<Item>
        {
            new("A", 1, DateTime.MinValue),
            new("B", 1, DateTime.MinValue),
            new("C", 2, DateTime.MinValue)
        }.AsReadOnly();

        var resultado = datosConEmpate.AplicarOrden(new OrdenDeReporte("Valor", "asc"));

        // Los dos items con Valor=1 deben mantener su orden relativo original (A antes que B)
        Assert.Equal("A", resultado[0].Nombre);
        Assert.Equal("B", resultado[1].Nombre);
        Assert.Equal("C", resultado[2].Nombre);
    }

    // ---- Tipos nullable y record struct ----

    private sealed record Punto(int X, int? Y);

    [Fact]
    public void PropiedadNullable_OrdenaCorrectamente()
    {
        var datos = new List<Punto>
        {
            new(1, 3),
            new(2, null),
            new(3, 1)
        }.AsReadOnly();

        var resultado = datos.AplicarOrden(new OrdenDeReporte("Y", "asc"));

        // null va primero en OrderBy ascendente
        Assert.Null(resultado[0].Y);
        Assert.Equal(1, resultado[1].Y);
        Assert.Equal(3, resultado[2].Y);
    }

    // ---- Lista vacía ----

    [Fact]
    public void ListaVacia_DevuelveVacia()
    {
        var vacia = new List<Item>().AsReadOnly();
        var resultado = vacia.AplicarOrden(new OrdenDeReporte("Nombre", "asc"));
        Assert.Empty(resultado);
    }

    // ---- Un solo elemento ----

    [Fact]
    public void UnSoloElemento_DevuelveMismoElemento()
    {
        var uno = new List<Item> { new("Solo", 42, DateTime.Now) }.AsReadOnly();
        var resultado = uno.AplicarOrden(new OrdenDeReporte("Nombre", "desc"));
        Assert.Single(resultado);
        Assert.Equal("Solo", resultado[0].Nombre);
    }
}
