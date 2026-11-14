using Menus.Api.Auth;
using Menus.Application.Comun;
using Menus.Application.Valoraciones;
using Menus.Application.Valoraciones.ListarValoracionesPorDieta;
using Menus.Application.Valoraciones.ListarValoracionesPorPaciente;
using Menus.Application.Valoraciones.ObtenerValoracion;
using Menus.Application.Valoraciones.RegistrarValoracion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Controladores;

/// <summary>
/// Valoraciones nutricionales (#86): la evaluación de un nutricionista sobre la asignación de un menú a un
/// paciente. Cada acción arma el comando o la consulta y lo envía al despachador (ADR-010, §8); los errores los
/// traduce el manejador global.
/// </summary>
/// <remarks>
/// No hay <c>PUT</c> ni <c>DELETE</c>: una valoración no cambia (R7-33) y no se borra. Todas las acciones son para un
/// nutricionista (#74). Que el propio paciente liste las suyas queda para el panel del paciente (#88), que tiene que
/// comprobar que sea él.
/// </remarks>
[ApiController]
[Authorize(Policy = Politicas.SoloNutricionista)]
[Route("api/valoraciones")]
[Produces("application/json")]
public sealed class ValoracionesController(IDespachador despachador) : ControllerBase
{
    /// <summary>Registra la valoración de la asignación de un menú a un paciente.</summary>
    /// <response code="201">Creada; <c>Location</c> apunta a la valoración.</response>
    /// <response code="404">El menú no existe, o el paciente no tiene ese menú asignado.</response>
    /// <response code="422">La entrada no es válida (por ejemplo, un resultado nutricional que no existe).</response>
    [HttpPost]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IdCreadoDto>> Registrar(CuerpoDeValoracion cuerpo, CancellationToken ct)
    {
        var creada = await despachador.EnviarAsync(
            new RegistrarValoracionCommand(cuerpo.MenuId, cuerpo.PacienteId, cuerpo.ResultadoNutricionalId), ct);
        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    /// <summary>Detalle de una valoración, con su resultado expandido.</summary>
    /// <response code="200">La valoración.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ValoracionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ValoracionDto> Obtener(Guid id, CancellationToken ct)
        => despachador.EnviarAsync(new ObtenerValoracionQuery(id), ct);

    /// <summary>Valoraciones de los menús de una dieta, de todos sus pacientes inscritos, de la más reciente a la más antigua.</summary>
    /// <response code="200">Las valoraciones.</response>
    /// <response code="404">La dieta no existe.</response>
    [HttpGet("~/api/dietas/{dietaId:guid}/valoraciones")]
    [ProducesResponseType<IReadOnlyList<ValoracionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<ValoracionDto>> ListarPorDieta(Guid dietaId, CancellationToken ct)
        => despachador.EnviarAsync(new ListarValoracionesPorDietaQuery(dietaId), ct);

    /// <summary>Valoraciones de un paciente, de todas sus dietas, de la más reciente a la más antigua.</summary>
    /// <response code="200">Las valoraciones.</response>
    /// <response code="404">El paciente no existe.</response>
    [HttpGet("~/api/pacientes/{pacienteId:guid}/valoraciones")]
    [ProducesResponseType<IReadOnlyList<ValoracionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<ValoracionDto>> ListarPorPaciente(Guid pacienteId, CancellationToken ct)
        => despachador.EnviarAsync(new ListarValoracionesPorPacienteQuery(pacienteId), ct);
}
