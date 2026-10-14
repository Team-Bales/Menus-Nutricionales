using Menus.Domain.Entidades;

namespace Menus.Domain.Tests.Entidades;

public sealed class ValoracionNutricionalTests
{
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();
        var menuId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var nutricionistaId = Guid.NewGuid();
        var resultadoNutricionalId = Guid.NewGuid();

        var valoracion = new ValoracionNutricional(id, menuId, pacienteId, nutricionistaId, resultadoNutricionalId);

        Assert.Equal(id, valoracion.Id);
        Assert.Equal(menuId, valoracion.MenuId);
        Assert.Equal(pacienteId, valoracion.PacienteId);
        Assert.Equal(nutricionistaId, valoracion.NutricionistaId);
        Assert.Equal(resultadoNutricionalId, valoracion.ResultadoNutricionalId);
    }

    [Fact]
    public void Equality_SameId_ReturnsTrue()
    {
        var id = Guid.NewGuid();

        var primera = new ValoracionNutricional(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var segunda = new ValoracionNutricional(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(primera, segunda);
    }
}
