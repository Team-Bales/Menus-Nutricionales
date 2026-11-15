using Menus.Api.Auth;
using Menus.Application.Catalogo.Alimentos;
using Menus.Application.Catalogo.Alimentos.ActualizarAlimento;
using Menus.Application.Catalogo.Alimentos.EliminarAlimento;
using Menus.Application.Catalogo.Alimentos.ListarAlimentos;
using Menus.Application.Catalogo.Alimentos.ObtenerAlimento;
using Menus.Application.Catalogo.Alimentos.RegistrarAlimento;
using Menus.Application.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Controladores;

/// <summary>
/// Banco de alimentos (#61). Cada acción arma el comando o la consulta y lo envía al despachador (ADR-010, §8); los
/// errores los traduce el manejador global.
/// </summary>
[ApiController]
[Route("api/alimentos")]
[Produces("application/json")]
public sealed class AlimentosController(IDespachador despachador) : ControllerBase
{
    /// <summary>Registra un alimento. Quien lo describe es el actor de la petición.</summary>
    /// <response code="201">Creado; <c>Location</c> apunta al alimento.</response>
    /// <response code="409">Ya hay un alimento o un plato con ese nombre (RC-01).</response>
    /// <response code="422">La entrada no es válida.</response>
    [Authorize(Policy = Politicas.SoloNutricionista)]
    [HttpPost]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IdCreadoDto>> Registrar(RegistrarAlimentoCommand comando, CancellationToken ct)
    {
        var creado = await despachador.EnviarAsync(comando, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>Lista los alimentos por páginas, con filtros opcionales.</summary>
    /// <param name="grupo">Grupo nutricional.</param>
    /// <param name="tipo">Tipo de preparación.</param>
    /// <param name="nivel">Nivel calórico.</param>
    /// <param name="page">Página, desde 1.</param>
    /// <param name="size">Alimentos por página, de 1 a 100.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">La página, en el sobre <c>{ data, total, page, size }</c>.</response>
    /// <response code="422">Los filtros o la paginación no son válidos.</response>
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<AlimentoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<ResultadoPaginado<AlimentoDto>> Listar(
        Guid? grupo,
        Guid? tipo,
        Guid? nivel,
        CancellationToken ct,
        int page = 1,
        int size = 20)
        => despachador.EnviarAsync(new ListarAlimentosQuery(grupo, tipo, nivel, page, size), ct);

    /// <summary>Detalle de un alimento.</summary>
    /// <response code="200">El alimento.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AlimentoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<AlimentoDto> Obtener(Guid id, CancellationToken ct)
        => despachador.EnviarAsync(new ObtenerAlimentoQuery(id), ct);

    /// <summary>Cambia la clasificación de un alimento. Solo puede hacerlo quien lo describió (RS-02).</summary>
    /// <response code="204">Actualizado.</response>
    /// <response code="404">No existe.</response>
    /// <response code="409">El actor no es quien describió el alimento (RS-02).</response>
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
            new ActualizarAlimentoCommand(id, cuerpo.GrupoNutricionalId, cuerpo.TipoPreparacionId, cuerpo.NivelCaloricoId),
            ct);
        return NoContent();
    }

    /// <summary>Elimina un alimento que no está en uso.</summary>
    /// <response code="204">Eliminado.</response>
    /// <response code="404">No existe.</response>
    /// <response code="409">Es ingrediente de un plato (RR-04) o figura en un menú (RR-22).</response>
    [Authorize(Policy = Politicas.SoloNutricionista)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await despachador.EnviarAsync(new EliminarAlimentoCommand(id), ct);
        return NoContent();
    }
}
