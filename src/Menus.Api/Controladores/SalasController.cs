using Menus.Application.Comun;
using Menus.Application.Salas;
using Menus.Application.Salas.ActualizarSala;
using Menus.Application.Salas.EliminarSala;
using Menus.Application.Salas.ListarSalas;
using Menus.Application.Salas.ObtenerSala;
using Menus.Application.Salas.RegistrarSala;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Controladores;

/// <summary>
/// CRUD de salas (#72). Cada acción arma el comando o la consulta y lo envía al despachador (ADR-010, §8); los
/// errores los traduce el manejador global.
/// </summary>
[ApiController]
[Route("api/salas")]
[Produces("application/json")]
public sealed class SalasController(IDespachador despachador) : ControllerBase
{
    /// <summary>Lista todas las salas (son pocas, no se pagina).</summary>
    /// <response code="200">Las salas.</response>
    /// <response code="401">No autenticado.</response>
    [HttpGet]
    [Authorize(Roles = "Admin,Nutricionista")]
    [ProducesResponseType<IReadOnlyList<SalaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public Task<IReadOnlyList<SalaDto>> Listar(CancellationToken ct)
        => despachador.EnviarAsync(new ListarSalasQuery(), ct);

    /// <summary>Detalle de una sala.</summary>
    /// <response code="200">La sala.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Nutricionista")]
    [ProducesResponseType<SalaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<SalaDto> Obtener(Guid id, CancellationToken ct)
        => despachador.EnviarAsync(new ObtenerSalaQuery(id), ct);

    /// <summary>Registra una sala nueva.</summary>
    /// <response code="201">Creada; <c>Location</c> apunta a la sala.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No es administrador.</response>
    /// <response code="409">Ya existe una sala con ese nombre.</response>
    /// <response code="422">La entrada no es válida.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Registrar(CuerpoDeSala cuerpo, CancellationToken ct)
    {
        var creada = await despachador.EnviarAsync(new RegistrarSalaCommand(cuerpo.Nombre), ct);
        return Created($"/api/salas/{creada.Id}", creada);
    }

    /// <summary>Actualiza el nombre de una sala.</summary>
    /// <response code="204">Actualizada.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No es administrador.</response>
    /// <response code="404">No existe.</response>
    /// <response code="409">Ya existe otra sala con ese nombre.</response>
    /// <response code="422">La entrada no es válida.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Actualizar(Guid id, CuerpoDeSala cuerpo, CancellationToken ct)
    {
        await despachador.EnviarAsync(new ActualizarSalaCommand(id, cuerpo.Nombre), ct);
        return NoContent();
    }

    /// <summary>Elimina una sala, solo si no tiene pacientes asignados.</summary>
    /// <response code="204">Eliminada.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No es administrador.</response>
    /// <response code="404">No existe.</response>
    /// <response code="409">Tiene pacientes asignados.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await despachador.EnviarAsync(new EliminarSalaCommand(id), ct);
        return NoContent();
    }
}
