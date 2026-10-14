using Menus.Domain.Entidades;

namespace Menus.Domain.Tests.Entidades;

public sealed class ValoracionNutricionalTests
{
    private static readonly DateTime _fecha = DateTime.UtcNow;

    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();
        var menuId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var nutricionistaId = Guid.NewGuid();
        var resultadoNutricionalId = Guid.NewGuid();
        const string observaciones = "Sin novedades";

        var valoracion = new ValoracionNutricional(id, menuId, pacienteId, nutricionistaId, resultadoNutricionalId, _fecha, observaciones);

        Assert.Equal(id, valoracion.Id);
        Assert.Equal(menuId, valoracion.MenuId);
        Assert.Equal(pacienteId, valoracion.PacienteId);
        Assert.Equal(nutricionistaId, valoracion.NutricionistaId);
        Assert.Equal(resultadoNutricionalId, valoracion.ResultadoNutricionalId);
        Assert.Equal(_fecha, valoracion.FechaValoracion);
        Assert.Equal(observaciones, valoracion.Observaciones);
    }

    [Fact]
    public void Constructor_ObservacionesNull_SetsNull()
    {
        var valoracion = new ValoracionNutricional(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _fecha);

        Assert.Null(valoracion.Observaciones);
    }

    [Fact]
    public void Constructor_FechaNoUtc_ThrowsArgumentException()
    {
        var fechaLocal = DateTime.Now;

        Assert.Throws<ArgumentException>(() =>
            new ValoracionNutricional(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), fechaLocal));
    }

    [Fact]
    public void Equality_SameId_ReturnsTrue()
    {
        var id = Guid.NewGuid();

        var primera = new ValoracionNutricional(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _fecha);
        var segunda = new ValoracionNutricional(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _fecha);

        Assert.Equal(primera, segunda);
    }
}
