using Menus.Application.Catalogo.Dietas;
using Menus.Application.Catalogo.Dietas.ActualizarDieta;
using Menus.Application.Catalogo.Dietas.AgregarGrupoDieta;
using Menus.Application.Catalogo.Dietas.AgregarRestriccionDieta;
using Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;
using Menus.Application.Catalogo.Dietas.EliminarDieta;
using Menus.Application.Catalogo.Dietas.ListarDietas;
using Menus.Application.Catalogo.Dietas.ObtenerDieta;
using Menus.Application.Catalogo.Dietas.QuitarGrupoDieta;
using Menus.Application.Catalogo.Dietas.QuitarRestriccionDieta;
using Menus.Application.Catalogo.Dietas.RegistrarDieta;
using Menus.Application.Comun;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Controladores;

/// <summary>
/// Catálogo de dietas (#63): su cobertura de grupos nutricionales y sus restricciones alimentarias. Cada acción
/// arma el comando o la consulta y lo envía al despachador (ADR-010, §8); los errores los traduce el manejador
/// global.
/// </summary>
[ApiController]
[Route("api/dietas")]
[Produces("application/json")]
public sealed class DietasController(IDespachador despachador) : ControllerBase
{
    /// <summary>Registra una dieta con su cobertura de grupos nutricionales y sus restricciones.</summary>
    /// <response code="201">Creada; <c>Location</c> apunta a la dieta.</response>
    /// <response code="422">La entrada no es válida (por ejemplo, sin ningún grupo requerido, RK-07).</response>
    [HttpPost]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IdCreadoDto>> Registrar(RegistrarDietaCommand comando, CancellationToken ct)
    {
        var creada = await despachador.EnviarAsync(comando, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    /// <summary>Lista las dietas por páginas, con filtro opcional por programa de atención.</summary>
    /// <param name="programa">Programa de atención.</param>
    /// <param name="page">Página, desde 1.</param>
    /// <param name="size">Dietas por página, de 1 a 100.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">La página, en el sobre <c>{ data, total, page, size }</c>.</response>
    /// <response code="422">La paginación no es válida.</response>
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<DietaResumenDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<ResultadoPaginado<DietaResumenDto>> Listar(
        Guid? programa,
        CancellationToken ct,
        int page = 1,
        int size = 20)
        => despachador.EnviarAsync(new ListarDietasQuery(programa, page, size), ct);

    /// <summary>Detalle de una dieta, con sus grupos y restricciones expandidos.</summary>
    /// <response code="200">La dieta.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<DietaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<DietaDto> Obtener(Guid id, CancellationToken ct)
        => despachador.EnviarAsync(new ObtenerDietaQuery(id), ct);

    /// <summary>Actualiza el nombre y el programa de atención de una dieta.</summary>
    /// <response code="204">Actualizada.</response>
    /// <response code="404">No existe.</response>
    /// <response code="422">La entrada no es válida.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Actualizar(Guid id, DatosDeDieta cuerpo, CancellationToken ct)
    {
        await despachador.EnviarAsync(new ActualizarDietaCommand(id, cuerpo.Nombre, cuerpo.ProgramaAtencionId), ct);
        return NoContent();
    }

    /// <summary>Elimina una dieta que no tiene pacientes inscritos ni menús.</summary>
    /// <response code="204">Eliminada.</response>
    /// <response code="404">No existe.</response>
    /// <response code="409">Tiene pacientes inscritos (RR-12) o algún menú (RR-13).</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await despachador.EnviarAsync(new EliminarDietaCommand(id), ct);
        return NoContent();
    }

    /// <summary>Agrega un grupo nutricional a la cobertura de la dieta. Agregar uno ya cubierto no falla ni lo duplica.</summary>
    /// <response code="204">Cubierto.</response>
    /// <response code="404">La dieta no existe.</response>
    /// <response code="422">La entrada no es válida.</response>
    [HttpPost("{id:guid}/grupos")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AgregarGrupo(Guid id, CoberturaDeGrupo cuerpo, CancellationToken ct)
    {
        await despachador.EnviarAsync(new AgregarGrupoDietaCommand(id, cuerpo.GrupoNutricionalId, cuerpo.Tipo), ct);
        return NoContent();
    }

    /// <summary>Quita un grupo nutricional de la cobertura de la dieta. Quitar uno que no cubre no falla.</summary>
    /// <response code="204">Quitado.</response>
    /// <response code="404">La dieta no existe.</response>
    /// <response code="409">Es el único grupo requerido de la dieta (RK-07).</response>
    [HttpDelete("{id:guid}/grupos/{grupoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> QuitarGrupo(Guid id, Guid grupoId, CancellationToken ct)
    {
        await despachador.EnviarAsync(new QuitarGrupoDietaCommand(id, grupoId), ct);
        return NoContent();
    }

    /// <summary>Agrega una restricción alimentaria a la dieta. Agregar una ya presente no falla ni la duplica.</summary>
    /// <response code="204">Agregada.</response>
    /// <response code="404">La dieta no existe.</response>
    /// <response code="422">La entrada no es válida.</response>
    [HttpPost("{id:guid}/restricciones")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AgregarRestriccion(Guid id, RestriccionDeDieta cuerpo, CancellationToken ct)
    {
        await despachador.EnviarAsync(new AgregarRestriccionDietaCommand(id, cuerpo.RestriccionAlimentariaId), ct);
        return NoContent();
    }

    /// <summary>Quita una restricción alimentaria de la dieta. Quitar una que no tiene no falla.</summary>
    /// <response code="204">Quitada.</response>
    /// <response code="404">La dieta no existe.</response>
    [HttpDelete("{id:guid}/restricciones/{restriccionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> QuitarRestriccion(Guid id, Guid restriccionId, CancellationToken ct)
    {
        await despachador.EnviarAsync(new QuitarRestriccionDietaCommand(id, restriccionId), ct);
        return NoContent();
    }

    /// <summary>
    /// Disponibilidad calórica de la dieta (informe, «R1»): por cada nivel calórico, cuántos ítems del banco son
    /// compatibles con la dieta y cómo se distribuyen los ítems de sus menús finales. Servida desde Redis cuando
    /// está disponible (#101).
    /// </summary>
    /// <response code="200">La disponibilidad calórica.</response>
    /// <response code="404">La dieta no existe.</response>
    [HttpGet("{id:guid}/disponibilidad-calorica")]
    [ProducesResponseType<DisponibilidadCaloricaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<DisponibilidadCaloricaDto> ObtenerDisponibilidadCalorica(Guid id, CancellationToken ct)
        => despachador.EnviarAsync(new DisponibilidadCaloricaQuery(id), ct);
}
