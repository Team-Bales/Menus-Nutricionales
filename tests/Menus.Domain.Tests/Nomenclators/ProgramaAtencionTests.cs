using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Nomenclators;

public sealed class ProgramaAtencionTests
{
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();

        var programa = new ProgramaAtencion(id, "Nutrición Pediátrica");

        Assert.Equal(id, programa.Id);
        Assert.Equal("Nutrición Pediátrica", programa.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaArgumentException(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new ProgramaAtencion(Guid.NewGuid(), nombre));
    }
}
