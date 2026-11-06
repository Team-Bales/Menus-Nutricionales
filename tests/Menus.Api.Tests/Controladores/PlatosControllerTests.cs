using Menus.Api.Controladores;
using Menus.Application.Catalogo.Platos;
using Menus.Application.Catalogo.Platos.ActualizarPlato;
using Menus.Application.Catalogo.Platos.AgregarIngrediente;
using Menus.Application.Catalogo.Platos.EliminarPlato;
using Menus.Application.Catalogo.Platos.ListarPlatos;
using Menus.Application.Catalogo.Platos.ObtenerPlato;
using Menus.Application.Catalogo.Platos.QuitarIngrediente;
using Menus.Application.Catalogo.Platos.RegistrarPlato;
using Menus.Application.Comun;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Tests.Controladores;

/// <summary>
/// Pruebas de <see cref="PlatosController"/>: qué solicitud arma cada acción y qué código devuelve (ADR-010, §8).
/// </summary>
public sealed class PlatosControllerTests
{
    private static readonly Guid Grupo = Guid.NewGuid();
    private static readonly Guid Tipo = Guid.NewGuid();
    private static readonly Guid Nivel = Guid.NewGuid();

    private readonly DespachadorFalso _despachador = new();

    private PlatosController Controlador => new(_despachador);

    [Fact]
    public async Task Registrar_envia_el_comando_y_responde_201_hacia_Obtener()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = new IdCreadoDto(id);
        var comando = new RegistrarPlatoCommand("Ensalada", Grupo, Tipo, Nivel, [new IngredienteDelPlato(Guid.NewGuid(), 10m)]);

        var respuesta = await Controlador.Registrar(comando, CancellationToken.None);

        var creado = Assert.IsType<CreatedAtActionResult>(respuesta.Result);
        Assert.Equal(nameof(PlatosController.Obtener), creado.ActionName);
        Assert.Equal(id, creado.RouteValues!["id"]);
        Assert.Same(comando, _despachador.Recibida);
    }

    [Fact]
    public async Task Listar_arma_la_consulta_con_los_filtros_y_la_paginacion()
    {
        var pagina = new ResultadoPaginado<PlatoDto>([], 0, 3, 10);
        _despachador.Resultado = pagina;

        var respuesta = await Controlador.Listar(Grupo, Tipo, Nivel, CancellationToken.None, page: 3, size: 10);

        Assert.Same(pagina, respuesta);
        Assert.Equal(new ListarPlatosQuery(Grupo, Tipo, Nivel, 3, 10), _despachador.Recibida);
    }

    [Fact]
    public async Task Obtener_arma_la_consulta_con_el_id_de_la_ruta()
    {
        var id = Guid.NewGuid();
        var dto = new PlatoDetalleDto(id, "Ensalada", Grupo, Tipo, Nivel, Guid.NewGuid(), []);
        _despachador.Resultado = dto;

        var respuesta = await Controlador.Obtener(id, CancellationToken.None);

        Assert.Same(dto, respuesta);
        Assert.Equal(new ObtenerPlatoQuery(id), _despachador.Recibida);
    }

    [Fact]
    public async Task Actualizar_toma_el_id_de_la_ruta_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.Actualizar(id, new ClasificacionDeItem(Grupo, Tipo, Nivel), CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new ActualizarPlatoCommand(id, Grupo, Tipo, Nivel), _despachador.Recibida);
    }

    [Fact]
    public async Task Eliminar_envia_el_comando_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.Eliminar(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new EliminarPlatoCommand(id), _despachador.Recibida);
    }

    [Fact]
    public async Task AgregarIngrediente_toma_el_plato_de_la_ruta_y_el_ingrediente_del_cuerpo()
    {
        var id = Guid.NewGuid();
        var alimento = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.AgregarIngrediente(id, new IngredienteDelPlato(alimento, 75m), CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new AgregarIngredienteCommand(id, alimento, 75m), _despachador.Recibida);
    }

    [Fact]
    public async Task QuitarIngrediente_toma_el_plato_y_el_alimento_de_la_ruta()
    {
        var id = Guid.NewGuid();
        var alimento = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.QuitarIngrediente(id, alimento, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new QuitarIngredienteCommand(id, alimento), _despachador.Recibida);
    }
}
