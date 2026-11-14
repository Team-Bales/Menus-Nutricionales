using Menus.Api.Auth;
using Menus.Application.Comun;
using Menus.Application.Pacientes;
using Menus.Application.Pacientes.ActualizarPaciente;
using Menus.Application.Pacientes.DesinscribirDieta;
using Menus.Application.Pacientes.InscribirDieta;
using Menus.Application.Pacientes.ListarPacientes;
using Menus.Application.Pacientes.ObtenerPaciente;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Controladores;

/// <summary>
/// Pacientes y sus inscripciones en dietas (#73). Cada acción arma el comando o la consulta y lo envía al
/// despachador (ADR-010, §8); los errores los traduce el manejador global.
/// </summary>
/// <remarks>
/// No hay <c>POST /api/pacientes</c>: el alta se hace con la cuenta del paciente, en
/// <c>/api/auth/registro/paciente</c> (#70). Todas las acciones son para un nutricionista o el administrador (#74).
/// </remarks>
[ApiController]
[Authorize(Policy = Politicas.NutricionistaOAdmin)]
[Route("api/pacientes")]
[Produces("application/json")]
public sealed class PacientesController(IDespachador despachador) : ControllerBase
{
    /// <summary>Lista los pacientes por páginas, sin sus dietas, con filtros opcionales por sala y por dieta.</summary>
    /// <param name="sala">Sala de hospitalización.</param>
    /// <param name="dieta">Dieta en la que están inscritos.</param>
    /// <param name="page">Página, desde 1.</param>
    /// <param name="size">Pacientes por página, de 1 a 100.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">La página, en el sobre <c>{ data, total, page, size }</c>.</response>
    /// <response code="422">La paginación no es válida.</response>
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<PacienteDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<ResultadoPaginado<PacienteDto>> Listar(
        Guid? sala,
        Guid? dieta,
        CancellationToken ct,
        int page = 1,
        int size = 20)
        => despachador.EnviarAsync(new ListarPacientesQuery(sala, dieta, page, size), ct);

    /// <summary>Detalle de un paciente, con su sala y sus dietas inscritas (identificador y nombre).</summary>
    /// <response code="200">El paciente.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PacienteDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<PacienteDetalleDto> Obtener(Guid id, CancellationToken ct)
        => despachador.EnviarAsync(new ObtenerPacienteQuery(id), ct);

    /// <summary>Cambia el nombre, la edad y la sala de un paciente.</summary>
    /// <response code="204">Actualizado.</response>
    /// <response code="404">El paciente no existe, o la sala nueva no existe.</response>
    /// <response code="422">La entrada no es válida (por ejemplo, una edad negativa).</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Actualizar(Guid id, DatosDePaciente cuerpo, CancellationToken ct)
    {
        await despachador.EnviarAsync(new ActualizarPacienteCommand(id, cuerpo.Nombre, cuerpo.Edad, cuerpo.SalaId), ct);
        return NoContent();
    }

    /// <summary>Inscribe al paciente en una dieta. Si ya está inscrito, no hace nada.</summary>
    /// <response code="204">Inscrito, o ya lo estaba.</response>
    /// <response code="404">El paciente no existe, o la dieta no existe.</response>
    /// <response code="422">La entrada no es válida (sin dieta).</response>
    [HttpPost("{id:guid}/dietas")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> InscribirDieta(Guid id, InscripcionEnDieta cuerpo, CancellationToken ct)
    {
        await despachador.EnviarAsync(new InscribirDietaCommand(id, cuerpo.DietaId), ct);
        return NoContent();
    }

    /// <summary>Retira la inscripción del paciente en una dieta.</summary>
    /// <response code="204">Retirada.</response>
    /// <response code="404">El paciente no existe, o no está inscrito en esa dieta.</response>
    [HttpDelete("{id:guid}/dietas/{dietaId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DesinscribirDieta(Guid id, Guid dietaId, CancellationToken ct)
    {
        await despachador.EnviarAsync(new DesinscribirDietaCommand(id, dietaId), ct);
        return NoContent();
    }
}
