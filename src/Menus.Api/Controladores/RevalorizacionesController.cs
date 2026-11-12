using Menus.Application.Comun;
using Menus.Application.Revalorizaciones.ListarPendientes;
using Menus.Application.Revalorizaciones.RegistrarResultadoRevalorizacion;
using Menus.Application.Revalorizaciones.SolicitarRevalorizacion;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Controladores;

/// <summary>
/// Flujo de revalorización (#87): el paciente la solicita indicando al nutricionista ejecutante,
/// solo ese nutricionista registra el resultado (una nueva valoración generada, RS-10)
/// y se listan las pendientes por nutricionista.
/// </summary>
[ApiController]
[Route("api/revalorizaciones")]
[Produces("application/json")]
public sealed class RevalorizacionesController(IDespachador despachador) : ControllerBase
{
    /// <summary>Solicita una revalorización indicando la valoración que se revisa y el nutricionista ejecutor.</summary>
    /// <response code="201">Creada; <c>Location</c> apunta a la revalorización.</response>
    /// <response code="404">La valoración revisada o el nutricionista no existen.</response>
    /// <response code="422">El cuerpo no es válido.</response>
    [HttpPost]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IdCreadoDto>> Solicitar(CuerpoDeSolicitudDeRevalorizacion cuerpo, CancellationToken ct)
    {
        var comando = new SolicitarRevalorizacionCommand(cuerpo.ValoracionRevisadaId, cuerpo.NutricionistaId);
        var creado = await despachador.EnviarAsync(comando, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>Obtiene una revalorización por su identificador.</summary>
    /// <response code="200">La revalorización.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RevalorizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<RevalorizacionDto> Obtener(Guid id, CancellationToken ct)
        => despachador.EnviarAsync(new ObtenerRevalorizacionQuery(id), ct);

    /// <summary>Registra el resultado de una revalorización: la valoración nutricional generada por el nutricionista ejecutor.</summary>
    /// <response code="201">Resultado registrado; <c>Location</c> apunta a la valoración generada.</response>
    /// <response code="401">Falta la cabecera <c>X-Actor-Id</c>.</response>
    /// <response code="404">La revalorización no existe.</response>
    /// <response code="409">El actor no es el nutricionista ejecutante (RK-11) o la revalorización ya tiene resultado.</response>
    /// <response code="422">El resultado nutricional no es una categoría válida (RR-17).</response>
    [HttpPost("{id:guid}/resultado")]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IdCreadoDto>> RegistrarResultado(Guid id, CuerpoDeResultadoDeRevalorizacion cuerpo, CancellationToken ct)
    {
        var comando = new RegistrarResultadoRevalorizacionCommand(id, cuerpo.ResultadoNutricionalId);
        var creado = await despachador.EnviarAsync(comando, ct);
        return CreatedAtAction(nameof(ValoracionesController.Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>Lista las revalorizaciones pendientes de un nutricionista (sin valoración generada).</summary>
    /// <response code="200">Lista de revalorizaciones pendientes, ordenadas por fecha de solicitud.</response>
    /// <response code="404">El nutricionista no existe.</response>
    [HttpGet("nutricionistas/{nutricionistaId:guid}/revalorizaciones/pendientes")]
    [ProducesResponseType<IReadOnlyList<RevalorizacionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<RevalorizacionDto>> ListarPendientes(Guid nutricionistaId, CancellationToken ct)
        => despachador.EnviarAsync(new ListarRevalorizacionesPendientesQuery(nutricionistaId), ct);
}