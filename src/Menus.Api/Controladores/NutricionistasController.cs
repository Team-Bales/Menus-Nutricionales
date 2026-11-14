using Menus.Application.Comun;
using Menus.Application.Nutricionistas;
using Menus.Application.Nutricionistas.ActualizarNutricionista;
using Menus.Application.Nutricionistas.AsignarRolJefe;
using Menus.Application.Nutricionistas.AutorizarDieta;
using Menus.Application.Nutricionistas.ListarNutricionistas;
using Menus.Application.Nutricionistas.ObtenerNutricionista;
using Menus.Application.Nutricionistas.RevocarDieta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Controladores;

/// <summary>
/// Gestión de nutricionistas y de la relación <c>Autoriza</c> con las dietas (#71). Cada acción arma el comando o la
/// consulta y lo envía al despachador (ADR-010, §8); los errores los traduce el manejador global.
/// </summary>
/// <remarks>No hay <c>POST /api/nutricionistas</c>: el registro se hace por <c>/api/auth/registro/nutricionista</c>.</remarks>
[ApiController]
[Route("api/nutricionistas")]
[Produces("application/json")]
public sealed class NutricionistasController(IDespachador despachador) : ControllerBase
{
    /// <summary>Lista por páginas los nutricionistas, con filtro opcional por especialidad.</summary>
    /// <param name="especialidadId">Solo los de esta especialidad, si se indica.</param>
    /// <param name="page">Página, desde 1.</param>
    /// <param name="size">Nutricionistas por página, de 1 a 100.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">La página, en el sobre <c>{ data, total, page, size }</c>.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No es administrador.</response>
    /// <response code="422">La paginación no es válida.</response>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<ResultadoPaginado<NutricionistaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<ResultadoPaginado<NutricionistaDto>> Listar(Guid? especialidadId, CancellationToken ct, int page = 1, int size = 20)
        => despachador.EnviarAsync(new ListarNutricionistasQuery(especialidadId, page, size), ct);

    /// <summary>Detalle de un nutricionista, con sus dietas autorizadas.</summary>
    /// <response code="200">El nutricionista.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No es Admin ni el propio nutricionista.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType<NutricionistaDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<NutricionistaDetalleDto> Obtener(Guid id, CancellationToken ct)
        => despachador.EnviarAsync(new ObtenerNutricionistaQuery(id), ct);

    /// <summary>Actualiza el nombre y la especialidad del nutricionista.</summary>
    /// <response code="204">Actualizado.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No es administrador.</response>
    /// <response code="404">No existe.</response>
    /// <response code="422">La entrada no es válida.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Actualizar(Guid id, CuerpoDeActualizacionDeNutricionista cuerpo, CancellationToken ct)
    {
        await despachador.EnviarAsync(new ActualizarNutricionistaCommand(id, cuerpo.Nombre, cuerpo.EspecialidadId), ct);
        return NoContent();
    }

    /// <summary>Autoriza al nutricionista para la dieta (relación <c>Autoriza</c>); idempotente.</summary>
    /// <response code="204">Autorizado (o ya lo estaba).</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No es administrador.</response>
    /// <response code="404">El nutricionista o la dieta no existen.</response>
    /// <response code="422">Falta la dieta.</response>
    [HttpPost("{id:guid}/dietas")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AutorizarDieta(Guid id, CuerpoDeAutorizacionDeDieta cuerpo, CancellationToken ct)
    {
        await despachador.EnviarAsync(new AutorizarDietaCommand(id, cuerpo.DietaId), ct);
        return NoContent();
    }

    /// <summary>Revoca la autorización del nutricionista para la dieta.</summary>
    /// <response code="204">Revocada.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No es administrador.</response>
    /// <response code="404">El nutricionista no existe.</response>
    /// <response code="409">El nutricionista no estaba autorizado para esa dieta.</response>
    [HttpDelete("{id:guid}/dietas/{dietaId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevocarDieta(Guid id, Guid dietaId, CancellationToken ct)
    {
        await despachador.EnviarAsync(new RevocarDietaCommand(id, dietaId), ct);
        return NoContent();
    }

    /// <summary>Designa al nutricionista como jefe de nutrición (RD-18); idempotente.</summary>
    /// <response code="204">Designado (o ya lo era).</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No es administrador.</response>
    /// <response code="404">No existe.</response>
    [HttpPost("{id:guid}/rol-jefe")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AsignarRolJefe(Guid id, CancellationToken ct)
    {
        await despachador.EnviarAsync(new AsignarRolJefeCommand(id), ct);
        return NoContent();
    }
}
