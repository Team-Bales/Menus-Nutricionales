using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;
using Menus.Domain.ValueObjects;

namespace Menus.Domain.Tests.ValueObjects;

public sealed class ProporcionTipoPreparacionTests
{
    [Fact]
    public void Crear_TipoValidoYProporcionValida_RetornaValueObject()
    {
        var proporcion = ProporcionTipoPreparacion.Crear(TipoPreparacion.PlatoFuerte.Id, 0.5m);

        Assert.Equal(TipoPreparacion.PlatoFuerte.Id, proporcion.TipoPreparacionId);
        Assert.Equal(0.5m, proporcion.Valor);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Crear_VariasProporcionesValidas_SonValidas(decimal valor)
    {
        var proporcion = ProporcionTipoPreparacion.Crear(TipoPreparacion.Entrante.Id, valor);

        Assert.Equal(valor, proporcion.Valor);
    }

    [Fact]
    public void Crear_TipoPreparacionInexistente_LanzaDatoInvalidoException()
    {
        var tipoInexistente = Guid.NewGuid();

        Assert.Throws<DatoInvalidoException>(() => ProporcionTipoPreparacion.Crear(tipoInexistente, 0.5m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(-1)]
    [InlineData(1.001)]
    [InlineData(2)]
    public void Crear_ProporcionFueraDeRango_LanzaDatoInvalidoException(decimal valor)
    {
        Assert.Throws<DatoInvalidoException>(() => ProporcionTipoPreparacion.Crear(TipoPreparacion.PlatoFuerte.Id, valor));
    }

    [Fact]
    public void Igualdad_MismoTipoYValor_SonIguales()
    {
        var p1 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Entrante.Id, 0.25m);
        var p2 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Entrante.Id, 0.25m);

        Assert.Equal(p1, p2);
        Assert.True(p1 == p2);
    }

    [Fact]
    public void Igualdad_DistintoTipo_NoSonIguales()
    {
        var p1 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Entrante.Id, 0.25m);
        var p2 = ProporcionTipoPreparacion.Crear(TipoPreparacion.PlatoFuerte.Id, 0.25m);

        Assert.NotEqual(p1, p2);
        Assert.True(p1 != p2);
    }

    [Fact]
    public void Igualdad_DistintoValor_NoSonIguales()
    {
        var p1 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Entrante.Id, 0.25m);
        var p2 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Entrante.Id, 0.5m);

        Assert.NotEqual(p1, p2);
        Assert.True(p1 != p2);
    }
}
