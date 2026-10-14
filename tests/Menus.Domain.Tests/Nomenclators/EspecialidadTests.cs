using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Nomenclators;

public sealed class EspecialidadTests
{
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();

        var especialidad = new Especialidad(id, "Dietética Clínica");

        Assert.Equal(id, especialidad.Id);
        Assert.Equal("Dietética Clínica", especialidad.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaArgumentException(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Especialidad(Guid.NewGuid(), nombre));
    }
}
