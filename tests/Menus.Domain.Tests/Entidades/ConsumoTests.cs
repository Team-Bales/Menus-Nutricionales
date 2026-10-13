using Menus.Domain.Entidades;

namespace Menus.Domain.Tests.Entidades;

public sealed class ConsumoTests
{
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var menuId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var alimentoId = Guid.NewGuid();

        var consumo = new Consumo(menuId, pacienteId, alimentoId, true);

        Assert.Equal(menuId, consumo.MenuId);
        Assert.Equal(pacienteId, consumo.PacienteId);
        Assert.Equal(alimentoId, consumo.AlimentoId);
        Assert.True(consumo.Aceptacion);
    }

    [Fact]
    public void Equality_SameCompositeKey_ReturnsTrue()
    {
        var menuId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var alimentoId = Guid.NewGuid();

        var aceptado = new Consumo(menuId, pacienteId, alimentoId, true);
        var rechazado = new Consumo(menuId, pacienteId, alimentoId, false);

        Assert.Equal(aceptado, rechazado);
    }

    [Fact]
    public void Equality_DifferentAlimentoId_ReturnsFalse()
    {
        var menuId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();

        var primero = new Consumo(menuId, pacienteId, Guid.NewGuid(), true);
        var segundo = new Consumo(menuId, pacienteId, Guid.NewGuid(), true);

        Assert.NotEqual(primero, segundo);
    }
}
