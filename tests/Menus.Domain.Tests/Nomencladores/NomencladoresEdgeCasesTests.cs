using Menus.Domain.Entities;
using Xunit;

namespace Menus.Domain.Tests.Nomencladores;

public class NomencladoresEdgeCasesTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void GrupoNutricional_NombreSoloEspacios_LanzaExcepcion(string nombreInvalido)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GrupoNutricional(Guid.NewGuid(), nombreInvalido));
    }

    [Fact]
    public void Especialidad_NombreConEspaciosAlRededor_SeGuardaTrimmed()
    {
        // Arrange
        const string nombreConEspacios = "   Nutrición Clínica   ";

        // Act
        var especialidad = new Especialidad(Guid.NewGuid(), nombreConEspacios);

        // Assert
        Assert.Equal("Nutrición Clínica", especialidad.Nombre);
    }

    [Fact]
    public void Sala_NombreConEspaciosAlRededor_SeGuardaTrimmed()
    {
        // Arrange
        const string nombreConEspacios = "   Sala A - Pediatría   ";

        // Act
        var sala = new Sala(Guid.NewGuid(), nombreConEspacios);

        // Assert
        Assert.Equal("Sala A - Pediatría", sala.Nombre);
    }
}
