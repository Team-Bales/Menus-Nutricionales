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

    [Fact]
    public void TipoPreparacion_Existe_SoloParaLosDelCatalogo()
    {
        Assert.All(TipoPreparacion.Todos, t => Assert.True(TipoPreparacion.Existe(t.Id)));
        Assert.False(TipoPreparacion.Existe(Guid.NewGuid()));
        Assert.False(TipoPreparacion.Existe(NivelCalorico.Bajo.Id));
    }

    [Fact]
    public void NivelCalorico_Existe_SoloParaLosDelCatalogo()
    {
        Assert.All(NivelCalorico.Todos, n => Assert.True(NivelCalorico.Existe(n.Id)));
        Assert.False(NivelCalorico.Existe(Guid.NewGuid()));
        Assert.False(NivelCalorico.Existe(TipoPreparacion.Postre.Id));
    }
}
