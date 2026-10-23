using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Entidades;

public sealed class AlimentoTests
{
    private readonly Guid _grupoId = Guid.NewGuid();
    private readonly Guid _nutricionistaId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();

        var alimento = new Alimento(id, "Manzana", _grupoId, TipoPreparacion.Postre.Id, NivelCalorico.Bajo.Id, _nutricionistaId);

        Assert.Equal(id, alimento.Id);
        Assert.Equal("Manzana", alimento.Nombre);
        Assert.Equal(_grupoId, alimento.GrupoNutricionalId);
        Assert.Equal(TipoPreparacion.Postre.Id, alimento.TipoPreparacionId);
        Assert.Equal(NivelCalorico.Bajo.Id, alimento.NivelCaloricoId);
        Assert.Equal(_nutricionistaId, alimento.NutricionistaId);
        Assert.IsAssignableFrom<ItemAlimentario>(alimento);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaArgumentException(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Alimento(Guid.NewGuid(), nombre, _grupoId, TipoPreparacion.Entrante.Id, NivelCalorico.Medio.Id, _nutricionistaId));
    }

    [Fact]
    public void Constructor_TipoPreparacionFueraDelCatalogo_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Alimento(Guid.NewGuid(), "Nombre", _grupoId, Guid.NewGuid(), NivelCalorico.Medio.Id, _nutricionistaId));
    }

    [Fact]
    public void Constructor_NivelCaloricoFueraDelCatalogo_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Alimento(Guid.NewGuid(), "Nombre", _grupoId, TipoPreparacion.Entrante.Id, Guid.NewGuid(), _nutricionistaId));
    }
}
