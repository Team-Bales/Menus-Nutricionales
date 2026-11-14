using Menus.Api.Auth;
using Menus.Application.Catalogo.Platos;
using Menus.Application.Catalogo.Platos.ActualizarPlato;
using Menus.Application.Catalogo.Platos.AgregarIngrediente;
using Menus.Application.Catalogo.Platos.EliminarPlato;
using Menus.Application.Catalogo.Platos.ListarPlatos;
using Menus.Application.Catalogo.Platos.ObtenerPlato;
using Menus.Application.Catalogo.Platos.QuitarIngrediente;
using Menus.Application.Catalogo.Platos.RegistrarPlato;
using Menus.Application.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Controladores;

/// <summary>
/// Platos del banco y su composición (#62). Cada acción arma el comando o la consulta y lo envía al despachador
/// (ADR-010, §8); los errores los traduce el manejador global.
/// </summary>
[ApiController]
[Route("api/platos")]
[Produces("application/json")]
public sealed class PlatosController(IDespachador despachador) : ControllerBase
{
    /// <summary>Registra un plato con su composición. Quien lo describe es el actor de la petición.</summary>
    /// <response code="201">Creado; <c>Location</c> apunta al plato.</response>
    /// <response code="409">
    /// Ya hay un ítem con ese nombre (RC-01), o un ingrediente no existe (RR-02) o es un plato (RS-12).
    /// </response>
    /// <response code="422">La entrada no es válida (por ejemplo, sin ingredientes o con una cantidad no positiva).</response>
    [Authorize(Policy = Politicas.SoloNutricionista)]
    [HttpPost]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IdCreadoDto>> Registrar(RegistrarPlatoCommand comando, CancellationToken ct)
    {
        var creado = await despachador.EnviarAsync(comando, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>Lista los platos por páginas, sin su composición, con filtros opcionales.</summary>
    /// <param name="grupo">Grupo nutricional.</param>
    /// <param name="tipo">Tipo de preparación.</param>
    /// <param name="nivel">Nivel calórico.</param>
    /// <param name="page">Página, desde 1.</param>
    /// <param name="size">Platos por página, de 1 a 100.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">La página, en el sobre <c>{ data, total, page, size }</c>.</response>
    /// <response code="422">Los filtros o la paginación no son válidos.</response>
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<PlatoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<ResultadoPaginado<PlatoDto>> Listar(
        Guid? grupo,
        Guid? tipo,
        Guid? nivel,
        CancellationToken ct,
        int page = 1,
        int size = 20)
        => despachador.EnviarAsync(new ListarPlatosQuery(grupo, tipo, nivel, page, size), ct);

    /// <summary>Detalle de un plato, con sus ingredientes (nombre y cantidad).</summary>
    /// <response code="200">El plato.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PlatoDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<PlatoDetalleDto> Obtener(Guid id, CancellationToken ct)
        => despachador.EnviarAsync(new ObtenerPlatoQuery(id), ct);

    /// <summary>Cambia la clasificación de un plato. Solo puede hacerlo quien lo describió (RS-02).</summary>
    /// <response code="204">Actualizado.</response>
    /// <response code="404">No existe.</response>
    /// <response code="409">El actor no es quien describió el plato (RS-02).</response>
    /// <response code="422">La clasificación no es válida.</response>
    [Authorize(Policy = Politicas.SoloNutricionista)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Actualizar(Guid id, ClasificacionDeItem cuerpo, CancellationToken ct)
    {
        await despachador.EnviarAsync(
            new ActualizarPlatoCommand(id, cuerpo.GrupoNutricionalId, cuerpo.TipoPreparacionId, cuerpo.NivelCaloricoId),
            ct);
        return NoContent();
    }

    /// <summary>Elimina un plato que no figura en ningún menú; su composición se borra con él.</summary>
    /// <response code="204">Eliminado.</response>
    /// <response code="404">No existe.</response>
    /// <response code="409">Figura en algún menú (RR-22).</response>
    [Authorize(Policy = Politicas.SoloNutricionista)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await despachador.EnviarAsync(new EliminarPlatoCommand(id), ct);
        return NoContent();
    }

    /// <summary>Agrega un alimento a la composición. Solo puede hacerlo quien describió el plato (RS-02).</summary>
    /// <response code="204">Agregado.</response>
    /// <response code="404">El plato no existe.</response>
    /// <response code="409">
    /// El actor no describió el plato (RS-02), el alimento no existe (RR-02), es un plato (RS-12) o ya está en la
    /// composición.
    /// </response>
    /// <response code="422">La entrada no es válida (por ejemplo, una cantidad no positiva).</response>
    [Authorize(Policy = Politicas.SoloNutricionista)]
    [HttpPost("{id:guid}/ingredientes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AgregarIngrediente(Guid id, IngredienteDelPlato cuerpo, CancellationToken ct)
    {
        await despachador.EnviarAsync(new AgregarIngredienteCommand(id, cuerpo.AlimentoId, cuerpo.Cantidad), ct);
        return NoContent();
    }

    /// <summary>Quita un alimento de la composición. No se quita el único (RK-03).</summary>
    /// <response code="204">Quitado.</response>
    /// <response code="404">El plato no existe, o el alimento no está en su composición.</response>
    /// <response code="409">El actor no describió el plato (RS-02), o es el único ingrediente (RK-03).</response>
    [Authorize(Policy = Politicas.SoloNutricionista)]
    [HttpDelete("{id:guid}/ingredientes/{alimentoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> QuitarIngrediente(Guid id, Guid alimentoId, CancellationToken ct)
    {
        await despachador.EnviarAsync(new QuitarIngredienteCommand(id, alimentoId), ct);
        return NoContent();
    }
}
