using Menus.Domain.Entidades;

namespace Menus.Domain.Tests.Entidades;

public sealed class NutricionistasDietaTests
{
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var nutricionistaId = Guid.NewGuid();
        var dietaId = Guid.NewGuid();

        var autorizacion = new NutricionistasDieta(nutricionistaId, dietaId);

        Assert.Equal(nutricionistaId, autorizacion.NutricionistaId);
        Assert.Equal(dietaId, autorizacion.DietaId);
    }

    [Fact]
    public void Equality_SameCompositeKey_ReturnsTrue()
    {
        var nutricionistaId = Guid.NewGuid();
        var dietaId = Guid.NewGuid();

        var a1 = new NutricionistasDieta(nutricionistaId, dietaId);
        var a2 = new NutricionistasDieta(nutricionistaId, dietaId);

        Assert.Equal(a1, a2);
        Assert.True(a1 == a2);
    }

    [Fact]
    public void Equality_DifferentCompositeKey_ReturnsFalse()
    {
        var a1 = new NutricionistasDieta(Guid.NewGuid(), Guid.NewGuid());
        var a2 = new NutricionistasDieta(Guid.NewGuid(), Guid.NewGuid());

        Assert.NotEqual(a1, a2);
    }
}
