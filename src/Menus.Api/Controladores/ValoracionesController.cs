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
/// No hay <c>PUT</c> ni <c>DELETE</c>: una valoración no cambia (R7-33) y no se borra. Registrar y listar por dieta son
/// para un nutricionista (#74). Las de un paciente las ven él mismo y el personal (#285): la lista, con la política
/// <see cref="Politicas.PacientePropioOPersonal"/>; el detalle, comprobado en el caso de uso, porque su ruta lleva el id
/// de la valoración y no el del paciente. Por eso la política va en cada acción y no en la clase.
/// </remarks>
[ApiController]
[Route("api/valoraciones")]
[Produces("application/json")]
public sealed class ValoracionesController(IDespachador despachador) : ControllerBase
{
    /// <summary>Registra la valoración de la asignación de un menú a un paciente.</summary>
    /// <response code="201">Creada; <c>Location</c> apunta a la valoración.</response>
    /// <response code="404">El menú no existe, o el paciente no tiene ese menú asignado.</response>
    /// <response code="422">La entrada no es válida (por ejemplo, un resultado nutricional que no existe).</response>
    [Authorize(Policy = Politicas.SoloNutricionista)]
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

    /// <summary>Detalle de una valoración, con su resultado expandido. La ven el paciente valorado y el personal.</summary>
    /// <response code="200">La valoración.</response>
    /// <response code="403">La pide un paciente que no es el valorado.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ValoracionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ValoracionDto> Obtener(Guid id, CancellationToken ct)
        => despachador.EnviarAsync(new ObtenerValoracionQuery(id), ct);

    /// <summary>Valoraciones de los menús de una dieta, de todos sus pacientes inscritos, de la más reciente a la más antigua.</summary>
    /// <response code="200">Las valoraciones.</response>
    /// <response code="404">La dieta no existe.</response>
    [Authorize(Policy = Politicas.SoloNutricionista)]
    [HttpGet("~/api/dietas/{dietaId:guid}/valoraciones")]
    [ProducesResponseType<IReadOnlyList<ValoracionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<ValoracionDto>> ListarPorDieta(Guid dietaId, CancellationToken ct)
        => despachador.EnviarAsync(new ListarValoracionesPorDietaQuery(dietaId), ct);

    /// <summary>
    /// Valoraciones de un paciente, de todas sus dietas, de la más reciente a la más antigua. Las ven el propio
    /// paciente y el personal.
    /// </summary>
    /// <param name="id">Identificador del paciente. Se llama <c>id</c> porque es el parámetro que mira la política.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">Las valoraciones.</response>
    /// <response code="403">El token es de otro paciente.</response>
    /// <response code="404">El paciente no existe.</response>
    [Authorize(Policy = Politicas.PacientePropioOPersonal)]
    [HttpGet("~/api/pacientes/{" + Politicas.ParametroDelPaciente + ":guid}/valoraciones")]
    [ProducesResponseType<IReadOnlyList<ValoracionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<ValoracionDto>> ListarPorPaciente(Guid id, CancellationToken ct)
        => despachador.EnviarAsync(new ListarValoracionesPorPacienteQuery(id), ct);
}
