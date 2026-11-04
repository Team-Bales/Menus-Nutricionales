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
    public void Crear_ColeccionVacia_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CoberturaGruposNutricionales.Crear([]));
    }

    [Fact]
    public void Crear_GrupoRepetido_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CoberturaGruposNutricionales.Crear([_grupo1, _grupo1]));
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
}
