using Menus.Domain.Excepciones;
using Menus.Domain.ValueObjects;

namespace Menus.Domain.Tests.ValueObjects;

public sealed class CantidadTotalAlimentosTests
{
    [Fact]
    public void Crear_ValorPositivo_RetornaValueObject()
    {
        var cantidad = CantidadTotalAlimentos.Crear(6);

        Assert.Equal(6, cantidad.Valor);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void Crear_VariosValoresPositivos_SonValidos(int valor)
    {
        var cantidad = CantidadTotalAlimentos.Crear(valor);

        Assert.Equal(valor, cantidad.Valor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Crear_ValorNoPositivo_LanzaDatoInvalidoException(int valor)
    {
        Assert.Throws<DatoInvalidoException>(() => CantidadTotalAlimentos.Crear(valor));
    }

    [Fact]
    public void ConversiónImplícita_AInt_RetornaValor()
    {
        var cantidad = CantidadTotalAlimentos.Crear(8);

        int valor = cantidad;

        Assert.Equal(8, valor);
    }

    [Fact]
    public void ConversiónExplícita_DesdeInt_CreaValueObject()
    {
        var cantidad = (CantidadTotalAlimentos)5;

        Assert.Equal(5, cantidad.Valor);
    }

    [Fact]
    public void Igualdad_MismoValor_SonIguales()
    {
        var c1 = CantidadTotalAlimentos.Crear(6);
        var c2 = CantidadTotalAlimentos.Crear(6);

        Assert.Equal(c1, c2);
        Assert.True(c1 == c2);
    }

    [Fact]
    public void Igualdad_DistintoValor_NoSonIguales()
    {
        var c1 = CantidadTotalAlimentos.Crear(6);
        var c2 = CantidadTotalAlimentos.Crear(8);

        Assert.NotEqual(c1, c2);
        Assert.True(c1 != c2);
    }

    [Fact]
    public void ToString_IncluyeValor()
    {
        var cantidad = CantidadTotalAlimentos.Crear(6);

        Assert.Contains("6", cantidad.ToString());
    }
}
