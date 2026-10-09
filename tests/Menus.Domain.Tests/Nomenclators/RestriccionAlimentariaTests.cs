using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Nomenclators;

public sealed class RestriccionAlimentariaTests
{
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();

        var restriccion = new RestriccionAlimentaria(id, "Sin gluten");

        Assert.Equal(id, restriccion.Id);
        Assert.Equal("Sin gluten", restriccion.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaArgumentException(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new RestriccionAlimentaria(Guid.NewGuid(), nombre));
    }
}
