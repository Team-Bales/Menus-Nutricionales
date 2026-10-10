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

        var alimento = new Alimento(id, "Manzana", _grupoId, TipoPreparacion.Postre, NivelCalorico.Bajo, _nutricionistaId);

        Assert.Equal(id, alimento.Id);
        Assert.Equal("Manzana", alimento.Nombre);
        Assert.Equal(_grupoId, alimento.GrupoNutricionalId);
        Assert.Equal(TipoPreparacion.Postre, alimento.TipoPreparacion);
        Assert.Equal(NivelCalorico.Bajo, alimento.NivelCalorico);
        Assert.Equal(_nutricionistaId, alimento.NutricionistaId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaArgumentException(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Alimento(Guid.NewGuid(), nombre, _grupoId, TipoPreparacion.Entrante, NivelCalorico.Medio, _nutricionistaId));
    }
}
