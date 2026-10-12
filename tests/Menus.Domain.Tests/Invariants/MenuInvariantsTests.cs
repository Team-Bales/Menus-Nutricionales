using Menus.Domain.Entities;
using Xunit;

namespace Menus.Domain.Tests.Invariants;

public class MenuInvariantsTests
{
    [Fact]
    public void Menu_SinParametros_NoPuedeGenerarse()
    {
        // Assert: Un menú no puede generarse con cantidad de alimentos <= 0 o referencias vacías
        Assert.Throws<ArgumentException>(() =>
            new Menu(
                Guid.NewGuid(),
                DateOnly.FromDateTime(DateTime.UtcNow),
                cantidadTotalAlimentos: 0,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            )
        );
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void MenuDistribucion_ProporcionFueraDeRango_LanzaExcepcion(decimal proporcionInvalida)
    {
        // Proporción debe ser strictly entre 0 y 1 (0 < p < 1)
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MenuDistribucion(Guid.NewGuid(), Enums.TipoPreparacion.PlatoFuerte, proporcionInvalida)
        );
    }

    [Fact]
    public void Menu_ExponeColeccionesDeSoloLectura()
    {
        // Arrange
        var menu = new Menu(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            cantidadTotalAlimentos: 5,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        );

        // Assert
        Assert.IsAssignableFrom<IReadOnlyCollection<MenuAlimento>>(menu.Alimentos);
        Assert.IsAssignableFrom<IReadOnlyCollection<MenuPlato>>(menu.Platos);
        Assert.IsAssignableFrom<IReadOnlyCollection<MenuDistribucion>>(menu.Distribuciones);
        Assert.IsAssignableFrom<IReadOnlyCollection<MenuGrupoNutricional>>(menu.GruposNutricionales);
    }
}