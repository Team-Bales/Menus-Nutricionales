using Menus.Domain.Enums;
using Xunit;

namespace Menus.Domain.Tests.Enums;

public class DomainEnumsTests
{
    [Fact]
    public void TipoPreparacion_TieneExactamenteCuatroValores()
    {
        // Arrange
        var expectedValues = new[] { "Entrante", "PlatoFuerte", "Postre", "Bebida" };
        var actualNames = Enum.GetNames<TipoPreparacion>();

        // Assert
        Assert.Equal(4, actualNames.Length);
        foreach (var expected in expectedValues)
        {
            Assert.Contains(expected, actualNames);
        }
    }

    [Fact]
    public void NivelCalorico_TieneExactamenteTresValores()
    {
        // Arrange
        var expectedValues = new[] { "Bajo", "Medio", "Alto" };
        var actualNames = Enum.GetNames<NivelCalorico>();

        // Assert
        Assert.Equal(3, actualNames.Length);
        foreach (var expected in expectedValues)
        {
            Assert.Contains(expected, actualNames);
        }
    }
}