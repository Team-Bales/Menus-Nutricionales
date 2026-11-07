using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;
using Menus.Domain.ValueObjects;

namespace Menus.Domain.Tests.ValueObjects;

public sealed class DistribucionTiposPreparacionTests
{
    private static readonly ProporcionTipoPreparacion Entrante25 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Entrante.Id, 0.25m);
    private static readonly ProporcionTipoPreparacion PlatoFuerte50 = ProporcionTipoPreparacion.Crear(TipoPreparacion.PlatoFuerte.Id, 0.5m);
    private static readonly ProporcionTipoPreparacion Postre25 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Postre.Id, 0.25m);
    private static readonly ProporcionTipoPreparacion Bebida05 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Bebida.Id, 0.05m);

    [Fact]
    public void Crear_ProporcionesValidasQueSumanUno_RetornaValueObject()
    {
        var distribucion = DistribucionTiposPreparacion.Crear([Entrante25, PlatoFuerte50, Postre25]);

        Assert.Equal(3, distribucion.Proporciones.Length);
        Assert.Contains(distribucion.Proporciones, p => p.TipoPreparacionId == TipoPreparacion.Entrante.Id && p.Valor == 0.25m);
        Assert.Contains(distribucion.Proporciones, p => p.TipoPreparacionId == TipoPreparacion.PlatoFuerte.Id && p.Valor == 0.5m);
        Assert.Contains(distribucion.Proporciones, p => p.TipoPreparacionId == TipoPreparacion.Postre.Id && p.Valor == 0.25m);
    }

    [Fact]
    public void Crear_ProporcionUnicaIgualAUno_EsValida()
    {
        var soloPlatoFuerte = ProporcionTipoPreparacion.Crear(TipoPreparacion.PlatoFuerte.Id, 1m);

        var distribucion = DistribucionTiposPreparacion.Crear([soloPlatoFuerte]);

        Assert.Single(distribucion.Proporciones);
        Assert.Equal(1m, distribucion.Proporciones.First().Valor);
    }

    [Fact]
    public void Crear_TresTerciosCalculados_EsValido()
    {
        var tercio = 1m / 3m;
        var p1 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Entrante.Id, tercio);
        var p2 = ProporcionTipoPreparacion.Crear(TipoPreparacion.PlatoFuerte.Id, tercio);
        var p3 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Postre.Id, tercio);

        var distribucion = DistribucionTiposPreparacion.Crear([p1, p2, p3]);

        Assert.Equal(3, distribucion.Proporciones.Length);
        Assert.NotEqual(1m, distribucion.Proporciones.Sum(p => p.Valor)); // 0.999...
    }

    [Fact]
    public void Crear_ProporcionesRedondeadasACuatroDecimales_EsValido()
    {
        var p1 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Entrante.Id, 0.3333m);
        var p2 = ProporcionTipoPreparacion.Crear(TipoPreparacion.PlatoFuerte.Id, 0.3333m);
        var p3 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Postre.Id, 0.3333m);

        var distribucion = DistribucionTiposPreparacion.Crear([p1, p2, p3]);

        Assert.Equal(0.9999m, distribucion.Proporciones.Sum(p => p.Valor));
    }

    [Fact]
    public void Crear_ColeccionNula_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DistribucionTiposPreparacion.Crear((IEnumerable<ProporcionTipoPreparacion>)null!));
    }

    [Fact]
    public void Crear_ColeccionVacia_LanzaDatoInvalidoException()
    {
        Assert.Throws<DatoInvalidoException>(() => DistribucionTiposPreparacion.Crear([]));
    }

    [Fact]
    public void Crear_TipoPreparacionRepetido_LanzaDatoInvalidoException()
    {
        var p1 = ProporcionTipoPreparacion.Crear(TipoPreparacion.PlatoFuerte.Id, 0.5m);
        var p2 = ProporcionTipoPreparacion.Crear(TipoPreparacion.PlatoFuerte.Id, 0.5m);

        Assert.Throws<DatoInvalidoException>(() => DistribucionTiposPreparacion.Crear([p1, p2]));
    }

    [Theory]
    [InlineData(0.3, 0.5)]  // suma 0.8
    [InlineData(0.6, 0.5)]  // suma 1.1
    [InlineData(0.5, 0.49)] // suma 0.99
    [InlineData(0.51, 0.5)] // suma 1.01
    public void Crear_ProporcionesQueNoSumanUno_LanzaDatoInvalidoException(decimal primera, decimal segunda)
    {
        var p1 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Entrante.Id, primera);
        var p2 = ProporcionTipoPreparacion.Crear(TipoPreparacion.PlatoFuerte.Id, segunda);

        Assert.Throws<DatoInvalidoException>(() => DistribucionTiposPreparacion.Crear([p1, p2]));
    }

    [Fact]
    public void CrearDesdeTuplas_ValidaYCreaValueObject()
    {
        IEnumerable<(Guid TipoPreparacionId, decimal Valor)> tuplas = [
            (TipoPreparacion.Entrante.Id, 0.25m),
            (TipoPreparacion.PlatoFuerte.Id, 0.5m),
            (TipoPreparacion.Postre.Id, 0.25m)
        ];

        var distribucion = DistribucionTiposPreparacion.CrearDesdeTuplas(tuplas);

        Assert.Equal(3, distribucion.Proporciones.Length);
    }

    [Fact]
    public void CrearDesdeTuplas_TipoInexistente_LanzaDatoInvalidoException()
    {
        Assert.Throws<DatoInvalidoException>(() => DistribucionTiposPreparacion.CrearDesdeTuplas([(Guid.NewGuid(), 1m)]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(1.5)]
    public void CrearDesdeTuplas_ProporcionFueraDeRango_LanzaDatoInvalidoException(decimal proporcion)
    {
        Assert.Throws<DatoInvalidoException>(() =>
            DistribucionTiposPreparacion.CrearDesdeTuplas([(TipoPreparacion.PlatoFuerte.Id, proporcion), (TipoPreparacion.Bebida.Id, 1m - proporcion)]));
    }

    [Fact]
    public void Igualdad_MismasProporciones_SonIguales()
    {
        var d1 = DistribucionTiposPreparacion.Crear([Entrante25, PlatoFuerte50, Postre25]);
        var d2 = DistribucionTiposPreparacion.Crear([Entrante25, PlatoFuerte50, Postre25]);

        Assert.Equal(d1, d2);
        Assert.True(d1 == d2);
    }

    [Fact]
    public void Igualdad_DistintasProporciones_NoSonIguales()
    {
        // d1: Entrante 0.25, PlatoFuerte 0.5, Postre 0.25 (suma 1)
        // d2: Entrante 0.25, PlatoFuerte 0.5, Bebida 0.25 (suma 1) - diferente tipo
        var bebida25 = ProporcionTipoPreparacion.Crear(TipoPreparacion.Bebida.Id, 0.25m);
        var d1 = DistribucionTiposPreparacion.Crear([Entrante25, PlatoFuerte50, Postre25]);
        var d2 = DistribucionTiposPreparacion.Crear([Entrante25, PlatoFuerte50, bebida25]);

        Assert.NotEqual(d1, d2);
        Assert.True(d1 != d2);
    }

    [Fact]
    public void AsReadOnly_RetornaLasMismasProporciones()
    {
        var distribucion = DistribucionTiposPreparacion.Crear([Entrante25, PlatoFuerte50, Postre25]);

        var soloLectura = distribucion.AsReadOnly();

        Assert.Equal(3, soloLectura.Count);
        Assert.Contains(Entrante25, soloLectura);
        Assert.Contains(PlatoFuerte50, soloLectura);
    }

    [Fact]
    public void Equals_ConNulo_EsFalso()
    {
        var distribucion = DistribucionTiposPreparacion.Crear([Entrante25, PlatoFuerte50, Postre25]);

        Assert.False(distribucion.Equals((DistribucionTiposPreparacion?)null));
    }

    [Fact]
    public void Equals_MismaReferencia_EsVerdadero()
    {
        var distribucion = DistribucionTiposPreparacion.Crear([Entrante25, PlatoFuerte50, Postre25]);

        Assert.True(distribucion.Equals(distribucion));
    }

    [Fact]
    public void Equals_DistintaCantidadDeProporciones_EsFalso()
    {
        var tres = DistribucionTiposPreparacion.Crear([Entrante25, PlatoFuerte50, Postre25]);
        var una = DistribucionTiposPreparacion.Crear([ProporcionTipoPreparacion.Crear(TipoPreparacion.PlatoFuerte.Id, 1m)]);

        Assert.False(tres.Equals(una));
    }

    [Fact]
    public void GetHashCode_MismasProporcionesEnDistintoOrden_Coincide()
    {
        var d1 = DistribucionTiposPreparacion.Crear([Entrante25, PlatoFuerte50, Postre25]);
        var d2 = DistribucionTiposPreparacion.Crear([Postre25, PlatoFuerte50, Entrante25]);

        Assert.Equal(d1.GetHashCode(), d2.GetHashCode());
    }
}
