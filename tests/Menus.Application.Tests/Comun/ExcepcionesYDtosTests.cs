using Menus.Application.Comun;

namespace Menus.Application.Tests.Comun;

/// <summary>
/// Pruebas de las excepciones y DTOs de la capa de aplicación (ADR-010, §2, §4, §6).
/// </summary>
public sealed class ExcepcionesYDtosTests
{
    [Fact]
    public void NoEncontradoException_constructor_asigna_recurso_e_id()
    {
        var id = Guid.NewGuid();
        var ex = new NoEncontradoException("alimento", id);

        Assert.Equal("alimento", ex.Recurso);
        Assert.Equal(id, ex.Id);
        Assert.Equal($"No existe el alimento {id}.", ex.Message);
    }

    [Fact]
    public void NoEncontradoException_con_id_no_guid_tambien_funciona()
    {
        var ex = new NoEncontradoException("menú", 42);

        Assert.Equal("menú", ex.Recurso);
        Assert.Equal(42, ex.Id);
        Assert.Equal("No existe el menú 42.", ex.Message);
    }

    [Fact]
    public void ActorNoIdentificadoException_constructor_asigna_mensaje()
    {
        var ex = new ActorNoIdentificadoException("Falta la cabecera X-Actor-Id.");

        Assert.Equal("Falta la cabecera X-Actor-Id.", ex.Message);
    }

    [Fact]
    public void ResultadoPaginado_constructor_y_propiedades()
    {
        var data = new[] { "a", "b", "c" };
        var resultado = new ResultadoPaginado<string>(data, 100, 2, 10);

        Assert.Equal(data, resultado.Data);
        Assert.Equal(100, resultado.Total);
        Assert.Equal(2, resultado.Page);
        Assert.Equal(10, resultado.Size);
    }

    [Fact]
    public void ResultadoPaginado_con_lista_vacia()
    {
        var resultado = new ResultadoPaginado<int>([], 0, 1, 20);

        Assert.Empty(resultado.Data);
        Assert.Equal(0, resultado.Total);
        Assert.Equal(1, resultado.Page);
        Assert.Equal(20, resultado.Size);
    }

    [Fact]
    public void IdCreadoDto_constructor_y_propiedad_id()
    {
        var id = Guid.NewGuid();
        var dto = new IdCreadoDto(id);

        Assert.Equal(id, dto.Id);
    }
}