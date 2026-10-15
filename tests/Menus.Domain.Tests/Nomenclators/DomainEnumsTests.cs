using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Nomenclators;

public sealed class DomainCatalogsTests
{
    [Fact]
    public void TipoPreparacion_TieneExactamenteCuatroInstancias()
    {
        Assert.Equal(4, TipoPreparacion.Todos.Count);
    }

    [Theory]
    [InlineData("Entrante")]
    [InlineData("Plato fuerte")]
    [InlineData("Postre")]
    [InlineData("Bebida")]
    public void TipoPreparacion_ContieneTodosLosNombresEsperados(string nombre)
    {
        Assert.Contains(TipoPreparacion.Todos, t => t.Nombre == nombre);
    }

    [Fact]
    public void TipoPreparacion_IdsUnicas()
    {
        var ids = TipoPreparacion.Todos.Select(t => t.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void NivelCalorico_TieneExactamenteTresInstancias()
    {
        Assert.Equal(3, NivelCalorico.Todos.Count);
    }

    [Theory]
    [InlineData("Bajo")]
    [InlineData("Medio")]
    [InlineData("Alto")]
    public void NivelCalorico_ContieneTodosLosNombresEsperados(string nombre)
    {
        Assert.Contains(NivelCalorico.Todos, n => n.Nombre == nombre);
    }

    [Fact]
    public void NivelCalorico_IdsUnicas()
    {
        var ids = NivelCalorico.Todos.Select(n => n.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
