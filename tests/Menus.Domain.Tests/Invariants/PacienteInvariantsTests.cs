using Menus.Domain.Entities;
using Xunit;

namespace Menus.Domain.Tests.Invariants;

public class PacienteInvariantsTests
{
    [Fact]
    public void Paciente_PuedeInscribirseEnVariasDietas()
    {
        // Arrange
        var paciente = new Paciente(Guid.NewGuid(), "Juan Pérez", 45, Guid.NewGuid());
        var dieta1Id = Guid.NewGuid();
        var dieta2Id = Guid.NewGuid();

        // Act
        paciente.InscribirDieta(dieta1Id);
        paciente.InscribirDieta(dieta2Id);

        // Assert
        Assert.Equal(2, paciente.Dietas.Count);
        Assert.Contains(paciente.Dietas, pd => pd.DietaId == dieta1Id);
        Assert.Contains(paciente.Dietas, pd => pd.DietaId == dieta2Id);
    }

    [Fact]
    public void Paciente_InscribirMismaDietaDosVeces_NoDuplicaRegistro()
    {
        // Arrange
        var paciente = new Paciente(Guid.NewGuid(), "María Gómez", 30, Guid.NewGuid());
        var dietaId = Guid.NewGuid();

        // Act
        paciente.InscribirDieta(dietaId);
        paciente.InscribirDieta(dietaId);

        // Assert
        Assert.Single(paciente.Dietas);
    }
}