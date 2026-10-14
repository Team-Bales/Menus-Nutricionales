using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Nomenclators;

public sealed class ResultadoNutricionalTests
{
    [Fact]
    public void Todos_TieneExactamenteCincoCategoriasOrdenadasPorValor()
    {
        var esperado = new[] { ("Muy mala", 1), ("Mala", 2), ("Normal", 3), ("Buena", 4), ("Excelente", 5) };

        Assert.Equal(esperado, ResultadoNutricional.Todos.Select(r => (r.Nombre, r.Valor)));
    }

    [Fact]
    public void Todos_NombresValoresEIdsSonUnicos()
    {
        var todos = ResultadoNutricional.Todos;

        Assert.Equal(todos.Count, todos.Select(r => r.Nombre).Distinct().Count());
        Assert.Equal(todos.Count, todos.Select(r => r.Valor).Distinct().Count());
        Assert.Equal(todos.Count, todos.Select(r => r.Id).Distinct().Count());
        Assert.DoesNotContain(todos, r => r.Id == Guid.Empty);
    }

    [Fact]
    public void Ids_SonFijos()
    {
        Assert.Equal(new Guid("e89977e7-eeb6-4d32-96e2-096ace535bc8"), ResultadoNutricional.MuyMala.Id);
        Assert.Equal(new Guid("4077627e-08b1-404a-aeb5-bb64a90538ba"), ResultadoNutricional.Mala.Id);
        Assert.Equal(new Guid("023f26c4-50c4-4e20-8c24-7d27b341dfd6"), ResultadoNutricional.Normal.Id);
        Assert.Equal(new Guid("c0cf2cb9-45bb-4a30-ab00-0c6a8bfc5ca1"), ResultadoNutricional.Buena.Id);
        Assert.Equal(new Guid("138dec6d-8202-4b54-831c-e6fd21508695"), ResultadoNutricional.Excelente.Id);
    }

    [Theory]
    [InlineData(1, "Muy mala")]
    [InlineData(3, "Normal")]
    [InlineData(5, "Excelente")]
    public void DesdeValor_ValorValido_DevuelveLaCategoria(int valor, string nombre)
    {
        var resultado = ResultadoNutricional.DesdeValor(valor);

        Assert.Equal(nombre, resultado.Nombre);
        Assert.Same(ResultadoNutricional.Todos[valor - 1], resultado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void DesdeValor_ValorFueraDeRango_LanzaArgumentOutOfRangeException(int valor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ResultadoNutricional.DesdeValor(valor));
    }
}
