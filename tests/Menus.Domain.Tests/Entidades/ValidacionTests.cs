using Menus.Domain.Entidades;

namespace Menus.Domain.Tests.Entidades;

public sealed class ValidacionTests
{
    [Fact]
    public void Constructor_WithoutObservaciones_SetsNullableProperty()
    {
        var nutricionistaId = Guid.NewGuid();
        var menuId = Guid.NewGuid();
        var fecha = DateTime.UtcNow;

        var validacion = new Validacion(nutricionistaId, menuId, fecha, true);

        Assert.Equal(nutricionistaId, validacion.NutricionistaId);
        Assert.Equal(menuId, validacion.MenuId);
        Assert.Equal(fecha, validacion.FechaValidacion);
        Assert.True(validacion.Aprobado);
        Assert.Null(validacion.Observaciones);
    }

    [Fact]
    public void Constructor_WithObservaciones_SetsAllProperties()
    {
        var fecha = DateTime.UtcNow;

        var validacion = new Validacion(Guid.NewGuid(), Guid.NewGuid(), fecha, false, "Ajustar la distribución.");

        Assert.False(validacion.Aprobado);
        Assert.Equal("Ajustar la distribución.", validacion.Observaciones);
    }

    [Fact]
    public void Equality_SameCompositeKey_ReturnsTrue()
    {
        var nutricionistaId = Guid.NewGuid();
        var menuId = Guid.NewGuid();

        var primera = new Validacion(nutricionistaId, menuId, DateTime.UtcNow, true);
        var segunda = new Validacion(nutricionistaId, menuId, DateTime.UtcNow.AddMinutes(1), false);

        Assert.Equal(primera, segunda);
    }
}
