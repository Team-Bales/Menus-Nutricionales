using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Entidades;

public sealed class ValoracionNutricionalTests
{
    private static readonly DateTime _fecha = DateTime.UtcNow;
    private static readonly Guid _buena = ResultadoNutricional.Buena.Id;

    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();
        var menuId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var nutricionistaId = Guid.NewGuid();

        var valoracion = new ValoracionNutricional(id, menuId, pacienteId, nutricionistaId, _buena, _fecha);

        Assert.Equal(id, valoracion.Id);
        Assert.Equal(menuId, valoracion.MenuId);
        Assert.Equal(pacienteId, valoracion.PacienteId);
        Assert.Equal(nutricionistaId, valoracion.NutricionistaId);
        Assert.Equal(_buena, valoracion.ResultadoNutricionalId);
        Assert.Equal(_fecha, valoracion.FechaValoracion);
    }

    [Fact]
    public void Constructor_CadaCategoriaDelCatalogo_SeAcepta()
    {
        foreach (var resultado in ResultadoNutricional.Todos)
        {
            var valoracion = new ValoracionNutricional(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), resultado.Id, _fecha);

            Assert.Equal(resultado.Id, valoracion.ResultadoNutricionalId);
        }
    }

    [Fact]
    public void Constructor_ResultadoFueraDelCatalogo_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new ValoracionNutricional(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _fecha));
    }

    [Fact]
    public void Constructor_FechaNoUtc_ThrowsArgumentException()
    {
        var fechaLocal = DateTime.Now;

        Assert.Throws<ArgumentException>(() =>
            new ValoracionNutricional(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _buena, fechaLocal));
    }

    [Fact]
    public void Equality_SameId_ReturnsTrue()
    {
        var id = Guid.NewGuid();

        var primera = new ValoracionNutricional(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _buena, _fecha);
        var segunda = new ValoracionNutricional(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ResultadoNutricional.Mala.Id, _fecha);

        Assert.Equal(primera, segunda);
    }
}
