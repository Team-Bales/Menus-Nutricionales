using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Nomenclators;

public sealed class DomainEnumsTests
{
    [Fact]
    public void TipoPreparacion_TieneExactamenteCuatroValores()
    {
        var expectedValues = new[] { "Entrante", "PlatoFuerte", "Postre", "Bebida" };
        var actualNames = Enum.GetNames<TipoPreparacion>();

        Assert.Equal(4, actualNames.Length);
        foreach (var expected in expectedValues)
        {
            Assert.Contains(expected, actualNames);
        }
    }

    [Fact]
    public void NivelCalorico_TieneExactamenteTresValores()
    {
        var expectedValues = new[] { "Bajo", "Medio", "Alto" };
        var actualNames = Enum.GetNames<NivelCalorico>();

        Assert.Equal(3, actualNames.Length);
        foreach (var expected in expectedValues)
        {
            Assert.Contains(expected, actualNames);
        }
    }
}
