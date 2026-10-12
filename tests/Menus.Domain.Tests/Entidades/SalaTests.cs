using Menus.Domain.Entidades;

namespace Menus.Domain.Tests.Entidades;

public sealed class SalaTests
{
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();

        var sala = new Sala(id, "Sala A");

        Assert.Equal(id, sala.Id);
        Assert.Equal("Sala A", sala.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaArgumentException(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Sala(Guid.NewGuid(), nombre));
    }
}
