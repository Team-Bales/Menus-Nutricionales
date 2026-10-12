using Menus.Domain.Entities;
using Xunit;

namespace Menus.Domain.Tests.Invariants;

public class DietaInvariantsTests
{
    [Fact]
    public void Dieta_AdmiteMultiplesRestriccionesAlimentarias()
    {
        // Arrange
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Hiposódica");
        var restriccion1 = Guid.NewGuid();
        var restriccion2 = Guid.NewGuid();

        // Act
        dieta.AgregarRestriccion(restriccion1);
        dieta.AgregarRestriccion(restriccion2);

        // Assert
        Assert.Equal(2, dieta.Restricciones.Count);
        Assert.Contains(dieta.Restricciones, r => r.RestriccionAlimentariaId == restriccion1);
        Assert.Contains(dieta.Restricciones, r => r.RestriccionAlimentariaId == restriccion2);
    }

    [Fact]
    public void Dieta_AdmiteMultiplesGruposNutricionales()
    {
        // Arrange
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Balanceada");
        var grupo1 = Guid.NewGuid();
        var grupo2 = Guid.NewGuid();

        // Act
        dieta.AgregarGrupoNutricional(grupo1);
        dieta.AgregarGrupoNutricional(grupo2);

        // Assert
        Assert.Equal(2, dieta.GruposNutricionales.Count);
        Assert.Contains(dieta.GruposNutricionales, g => g.GrupoNutricionalId == grupo1);
        Assert.Contains(dieta.GruposNutricionales, g => g.GrupoNutricionalId == grupo2);
    }
}