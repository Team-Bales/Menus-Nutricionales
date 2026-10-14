using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Entidades;

public sealed class PlatoTests
{
    private readonly Guid _grupoId = Guid.NewGuid();
    private readonly Guid _nutricionistaId = Guid.NewGuid();
    private readonly Guid _alimentoId1 = Guid.NewGuid();
    private readonly Guid _alimentoId2 = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();

        var plato = new Plato(id, "Ensalada César", _grupoId, TipoPreparacion.Entrante, NivelCalorico.Medio, _nutricionistaId);

        Assert.Equal(id, plato.Id);
        Assert.Equal("Ensalada César", plato.Nombre);
        Assert.Equal(_grupoId, plato.GrupoNutricionalId);
        Assert.Equal(TipoPreparacion.Entrante, plato.TipoPreparacion);
        Assert.Equal(NivelCalorico.Medio, plato.NivelCalorico);
        Assert.Equal(_nutricionistaId, plato.NutricionistaId);
        Assert.Empty(plato.Ingredientes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaArgumentException(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Plato(Guid.NewGuid(), nombre, _grupoId, TipoPreparacion.PlatoFuerte, NivelCalorico.Alto, _nutricionistaId));
    }

    [Fact]
    public void AgregarIngrediente_ValidArgs_AgregaYRetornaIngrediente()
    {
        var plato = new Plato(Guid.NewGuid(), "Plato Test", _grupoId, TipoPreparacion.PlatoFuerte, NivelCalorico.Medio, _nutricionistaId);

        var ingrediente = plato.AgregarIngrediente(_alimentoId1, 150m);

        Assert.Single(plato.Ingredientes);
        Assert.Equal(plato.Id, ingrediente.PlatoId);
        Assert.Equal(_alimentoId1, ingrediente.AlimentoId);
        Assert.Equal(150m, ingrediente.Cantidad);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public void AgregarIngrediente_CantidadNoPositiva_LanzaArgumentOutOfRangeException(decimal cantidad)
    {
        var plato = new Plato(Guid.NewGuid(), "Plato Test", _grupoId, TipoPreparacion.PlatoFuerte, NivelCalorico.Medio, _nutricionistaId);

        Assert.Throws<ArgumentOutOfRangeException>(() => plato.AgregarIngrediente(_alimentoId1, cantidad));
    }

    [Fact]
    public void AgregarIngrediente_AlimentoDuplicado_LanzaInvalidOperationException()
    {
        var plato = new Plato(Guid.NewGuid(), "Plato Test", _grupoId, TipoPreparacion.PlatoFuerte, NivelCalorico.Medio, _nutricionistaId);

        plato.AgregarIngrediente(_alimentoId1, 100m);

        Assert.Throws<InvalidOperationException>(() => plato.AgregarIngrediente(_alimentoId1, 200m));
    }

    [Fact]
    public void AgregarIngrediente_MultiplesIngredientes_TodosPresentes()
    {
        var plato = new Plato(Guid.NewGuid(), "Plato Test", _grupoId, TipoPreparacion.PlatoFuerte, NivelCalorico.Medio, _nutricionistaId);

        plato.AgregarIngrediente(_alimentoId1, 100m);
        plato.AgregarIngrediente(_alimentoId2, 200m);

        Assert.Equal(2, plato.Ingredientes.Count);
        Assert.Contains(plato.Ingredientes, i => i.AlimentoId == _alimentoId1 && i.Cantidad == 100m);
        Assert.Contains(plato.Ingredientes, i => i.AlimentoId == _alimentoId2 && i.Cantidad == 200m);
    }

    [Fact]
    public void Ingredientes_ReturnsReadOnlyCollection_NoPermiteModificacionExterna()
    {
        var plato = new Plato(Guid.NewGuid(), "Plato Test", _grupoId, TipoPreparacion.PlatoFuerte, NivelCalorico.Medio, _nutricionistaId);

        var collection = plato.Ingredientes;

        Assert.IsAssignableFrom<IReadOnlyCollection<PlatoIngrediente>>(collection);
        Assert.NotSame(plato.Ingredientes, collection); // nueva instancia cada acceso (AsReadOnly crea wrapper)
    }
}
