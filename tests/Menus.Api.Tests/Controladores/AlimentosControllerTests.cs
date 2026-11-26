using System.Text.Json.Nodes;
using Menus.Api.Controladores;
using Menus.Application.Catalogo.Alimentos;
using Menus.Application.Catalogo.Alimentos.ActualizarAlimento;
using Menus.Application.Catalogo.Alimentos.EliminarAlimento;
using Menus.Application.Catalogo.Alimentos.ListarAlimentos;
using Menus.Application.Catalogo.Alimentos.ObtenerAlimento;
using Menus.Application.Catalogo.Alimentos.RegistrarAlimento;
using Menus.Application.Catalogo.Fichas;
using Menus.Application.Catalogo.Fichas.GuardarFicha;
using Menus.Application.Catalogo.Fichas.ObtenerFicha;
using Menus.Application.Comun;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Tests.Controladores;

/// <summary>
/// Pruebas de <see cref="AlimentosController"/>: qué solicitud arma cada acción y qué código devuelve
/// (ADR-010, §8). Los errores los traduce el manejador global, que tiene sus propias pruebas.
/// </summary>
public sealed class AlimentosControllerTests
{
    private static readonly Guid Grupo = Guid.NewGuid();
    private static readonly Guid Tipo = Guid.NewGuid();
    private static readonly Guid Nivel = Guid.NewGuid();

    private readonly DespachadorFalso _despachador = new();

    private AlimentosController Controlador => new(_despachador);

    [Fact]
    public async Task ObtenerFicha_pide_la_ficha_del_alimento_de_la_ruta()
    {
        var id = Guid.NewGuid();
        var ficha = new FichaNutricionalDto(id, "Alimento", "simple", null, null, null, [], null, null, DateTime.UtcNow);
        _despachador.Resultado = ficha;

        var respuesta = await Controlador.ObtenerFicha(id, CancellationToken.None);

        Assert.Same(ficha, respuesta);
        Assert.Equal(new ObtenerFichaQuery(ClaseDeItem.Alimento, id), _despachador.Recibida);
    }

    [Fact]
    public async Task GuardarFicha_arma_el_comando_de_alimento_con_el_id_de_la_ruta()
    {
        var id = Guid.NewGuid();
        var macros = new JsonObject { ["energiaKcal"] = 52 };
        _despachador.Resultado = new FichaNutricionalDto(id, "Alimento", "simple", null, macros, null, null, null, null, DateTime.UtcNow);

        await Controlador.GuardarFicha(id, new CuerpoDeFicha(null, macros, null, null, null, null), CancellationToken.None);

        Assert.Equal(
            new GuardarFichaCommand(ClaseDeItem.Alimento, id, null, macros, null, null, null, null),
            _despachador.Recibida);
    }

    [Fact]
    public async Task Registrar_envia_el_comando_y_responde_201_hacia_Obtener()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = new IdCreadoDto(id);
        var comando = new RegistrarAlimentoCommand("Avena", Grupo, Tipo, Nivel);

        var respuesta = await Controlador.Registrar(comando, CancellationToken.None);

        var creado = Assert.IsType<CreatedAtActionResult>(respuesta.Result);
        Assert.Equal(nameof(AlimentosController.Obtener), creado.ActionName);
        Assert.Equal(id, creado.RouteValues!["id"]);
        Assert.Equal(new IdCreadoDto(id), creado.Value);
        Assert.Same(comando, _despachador.Recibida);
    }

    [Fact]
    public async Task Listar_arma_la_consulta_con_los_filtros_y_la_paginacion()
    {
        var pagina = new ResultadoPaginado<AlimentoDto>([], 0, 2, 5);
        _despachador.Resultado = pagina;

        var respuesta = await Controlador.Listar(Grupo, Tipo, Nivel, CancellationToken.None, page: 2, size: 5);

        Assert.Same(pagina, respuesta);
        Assert.Equal(new ListarAlimentosQuery(Grupo, Tipo, Nivel, 2, 5), _despachador.Recibida);
    }

    [Fact]
    public async Task Obtener_arma_la_consulta_con_el_id_de_la_ruta()
    {
        var id = Guid.NewGuid();
        var dto = new AlimentoDto(id, "Avena", Grupo, Tipo, Nivel, Guid.NewGuid());
        _despachador.Resultado = dto;

        var respuesta = await Controlador.Obtener(id, CancellationToken.None);

        Assert.Same(dto, respuesta);
        Assert.Equal(new ObtenerAlimentoQuery(id), _despachador.Recibida);
    }

    [Fact]
    public async Task Actualizar_toma_el_id_de_la_ruta_y_la_clasificacion_del_cuerpo_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.Actualizar(id, new ClasificacionDeItem(Grupo, Tipo, Nivel), CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new ActualizarAlimentoCommand(id, Grupo, Tipo, Nivel), _despachador.Recibida);
    }

    [Fact]
    public async Task Eliminar_envia_el_comando_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.Eliminar(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new EliminarAlimentoCommand(id), _despachador.Recibida);
    }
}
