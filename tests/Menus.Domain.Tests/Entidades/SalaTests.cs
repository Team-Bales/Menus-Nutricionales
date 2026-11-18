using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;

namespace Menus.Domain.Tests.Entidades;

public sealed class SalaTests
{
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();

        var sala = new Sala(id, "Sala A");

        Assert.Equal(id, sala.Id);
        Assert.Equal("Sala A", sala.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaDatoInvalidoException(string nombre)
    {
        Assert.Throws<DatoInvalidoException>(() => new Sala(Guid.NewGuid(), nombre));
    }

    [Fact]
    public void Actualizar_ValidArgs_CambiaElNombre()
    {
        var sala = new Sala(Guid.NewGuid(), "Sala A");

        sala.Actualizar("Sala A - Pediatría");

        Assert.Equal("Sala A - Pediatría", sala.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Actualizar_NombreVacio_LanzaDatoInvalidoException(string nombre)
    {
        var sala = new Sala(Guid.NewGuid(), "Sala A");

        Assert.Throws<DatoInvalidoException>(() => sala.Actualizar(nombre));
        Assert.Equal("Sala A", sala.Nombre);
    }
}
