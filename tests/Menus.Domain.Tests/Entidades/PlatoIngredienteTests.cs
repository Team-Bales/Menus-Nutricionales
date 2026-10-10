using Menus.Domain.Entidades;

namespace Menus.Domain.Tests.Entidades;

public sealed class PlatoIngredienteTests
{
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var platoId = Guid.NewGuid();
        var alimentoId = Guid.NewGuid();

        var ingrediente = new PlatoIngrediente(platoId, alimentoId, 100m);

        Assert.Equal(platoId, ingrediente.PlatoId);
        Assert.Equal(alimentoId, ingrediente.AlimentoId);
        Assert.Equal(100m, ingrediente.Cantidad);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public void Constructor_CantidadNoPositiva_LanzaArgumentOutOfRangeException(decimal cantidad)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlatoIngrediente(Guid.NewGuid(), Guid.NewGuid(), cantidad));
    }

    [Fact]
    public void Equality_SameCompositeKey_ReturnsTrue()
    {
        var platoId = Guid.NewGuid();
        var alimentoId = Guid.NewGuid();

        var i1 = new PlatoIngrediente(platoId, alimentoId, 50m);
        var i2 = new PlatoIngrediente(platoId, alimentoId, 75m); // distinta cantidad, misma clave

        Assert.Equal(i1, i2);
        Assert.True(i1 == i2);
    }

    [Fact]
    public void Equality_DifferentCompositeKey_ReturnsFalse()
    {
        var i1 = new PlatoIngrediente(Guid.NewGuid(), Guid.NewGuid(), 50m);
        var i2 = new PlatoIngrediente(Guid.NewGuid(), Guid.NewGuid(), 50m);

        Assert.NotEqual(i1, i2);
    }
}
