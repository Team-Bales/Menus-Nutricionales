using Menus.Domain.Excepciones;
using Menus.Domain.ValueObjects;

namespace Menus.Domain.Tests.ValueObjects;

public sealed class CoberturaGruposNutricionalesTests
{
    private readonly Guid _grupo1 = Guid.NewGuid();
    private readonly Guid _grupo2 = Guid.NewGuid();
    private readonly Guid _grupo3 = Guid.NewGuid();

    [Fact]
    public void Crear_UnGrupo_RetornaValueObject()
    {
        var cobertura = CoberturaGruposNutricionales.Crear([_grupo1]);

        Assert.Single(cobertura.GrupoIds);
        Assert.Contains(_grupo1, cobertura.GrupoIds);
    }

    [Fact]
    public void Crear_VariosGrupos_RetornaValueObject()
    {
        var cobertura = CoberturaGruposNutricionales.Crear([_grupo1, _grupo2, _grupo3]);

        Assert.Equal(3, cobertura.GrupoIds.Length);
        Assert.Contains(_grupo1, cobertura.GrupoIds);
        Assert.Contains(_grupo2, cobertura.GrupoIds);
        Assert.Contains(_grupo3, cobertura.GrupoIds);
    }

    [Fact]
    public void Crear_ColeccionNula_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CoberturaGruposNutricionales.Crear((IEnumerable<Guid>)null!));
    }

    [Fact]
    public void Crear_ColeccionVacia_LanzaDatoInvalidoException()
    {
        Assert.Throws<DatoInvalidoException>(() => CoberturaGruposNutricionales.Crear([]));
    }

    [Fact]
    public void Crear_GrupoRepetido_LanzaDatoInvalidoException()
    {
        Assert.Throws<DatoInvalidoException>(() => CoberturaGruposNutricionales.Crear([_grupo1, _grupo1]));
    }

    [Fact]
    public void Igualdad_MismosGrupos_SonIguales()
    {
        var c1 = CoberturaGruposNutricionales.Crear([_grupo1, _grupo2]);
        var c2 = CoberturaGruposNutricionales.Crear([_grupo1, _grupo2]);

        Assert.Equal(c1, c2);
        Assert.True(c1 == c2);
    }

    [Fact]
    public void Igualdad_DistintosGrupos_NoSonIguales()
    {
        var c1 = CoberturaGruposNutricionales.Crear([_grupo1, _grupo2]);
        var c2 = CoberturaGruposNutricionales.Crear([_grupo1, _grupo3]);

        Assert.NotEqual(c1, c2);
        Assert.True(c1 != c2);
    }

    [Fact]
    public void Igualdad_MismosGruposOrdenDistinto_SonIguales()
    {
        var c1 = CoberturaGruposNutricionales.Crear([_grupo1, _grupo2]);
        var c2 = CoberturaGruposNutricionales.Crear([_grupo2, _grupo1]);

        Assert.Equal(c1, c2);
        Assert.True(c1 == c2);
    }

    [Fact]
    public void AsReadOnly_RetornaLosMismosGrupos()
    {
        var cobertura = CoberturaGruposNutricionales.Crear([_grupo1, _grupo2]);

        var soloLectura = cobertura.AsReadOnly();

        Assert.Equal(2, soloLectura.Count);
        Assert.Contains(_grupo1, soloLectura);
        Assert.Contains(_grupo2, soloLectura);
    }

    [Fact]
    public void Equals_ConNulo_EsFalso()
    {
        var cobertura = CoberturaGruposNutricionales.Crear([_grupo1]);

        Assert.False(cobertura.Equals((CoberturaGruposNutricionales?)null));
    }

    [Fact]
    public void Equals_MismaReferencia_EsVerdadero()
    {
        var cobertura = CoberturaGruposNutricionales.Crear([_grupo1, _grupo2]);

        Assert.True(cobertura.Equals(cobertura));
    }

    [Fact]
    public void Equals_DistintaCantidadDeGrupos_EsFalso()
    {
        var dosGrupos = CoberturaGruposNutricionales.Crear([_grupo1, _grupo2]);
        var unGrupo = CoberturaGruposNutricionales.Crear([_grupo1]);

        Assert.False(dosGrupos.Equals(unGrupo));
    }

    [Fact]
    public void GetHashCode_MismosGruposEnDistintoOrden_Coincide()
    {
        var c1 = CoberturaGruposNutricionales.Crear([_grupo1, _grupo2]);
        var c2 = CoberturaGruposNutricionales.Crear([_grupo2, _grupo1]);

        Assert.Equal(c1.GetHashCode(), c2.GetHashCode());
    }
}
