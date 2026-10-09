using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Nomenclators;

public sealed class GrupoNutricionalTests
{
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();

        var grupo = new GrupoNutricional(id, "Lácteos");

        Assert.Equal(id, grupo.Id);
        Assert.Equal("Lácteos", grupo.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaArgumentException(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new GrupoNutricional(Guid.NewGuid(), nombre));
    }
}
