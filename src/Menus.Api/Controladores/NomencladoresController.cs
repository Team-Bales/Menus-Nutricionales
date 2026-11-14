using Menus.Api.Auth;
using Menus.Application.Catalogo.Nomencladores;
using Menus.Application.Catalogo.Nomencladores.Especialidades;
using Menus.Application.Catalogo.Nomencladores.GruposNutricionales;
using Menus.Application.Catalogo.Nomencladores.NivelesCaloricos;
using Menus.Application.Catalogo.Nomencladores.ProgramasAtencion;
using Menus.Application.Catalogo.Nomencladores.RestriccionesAlimentarias;
using Menus.Application.Catalogo.Nomencladores.TiposPreparacion;
using Menus.Application.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Controladores;

/// <summary>
/// Administración de los 4 nomencladores institucionales abiertos (#64): grupos nutricionales, restricciones
/// alimentarias, programas de atención y especialidades. Los catálogos cerrados (tipos de preparación y niveles
/// calóricos) son fijos: solo se consultan, para que el cliente cargue sus selectores desde la API (#65). Cada acción
/// arma el comando o la consulta y lo envía al despachador (ADR-010, §8); los errores los traduce el manejador global.
/// </summary>
/// <remarks>
/// En esta versión no hay autenticación todavía: los 4 nomencladores son accesibles sin restricción (el control
/// de acceso se agrega en v0.3). Cada nomenclador es un catálogo corto, así que <c>GET</c> devuelve la lista
/// completa, sin paginar.
/// </remarks>
[ApiController]
[Route("api/nomencladores")]
[Produces("application/json")]
public sealed class NomencladoresController(IDespachador despachador) : ControllerBase
{
    // ---- Catálogos cerrados (solo lectura) ----

    /// <summary>Los tipos de preparación del catálogo cerrado (RD-01).</summary>
    /// <response code="200">La lista, en el orden del catálogo.</response>
    [HttpGet("tipos-preparacion")]
    [ProducesResponseType<IReadOnlyList<NomencladorDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<NomencladorDto>> ListarTiposPreparacion(CancellationToken ct)
        => despachador.EnviarAsync(new ListarTiposPreparacionQuery(), ct);

    /// <summary>Los niveles calóricos del catálogo cerrado (RD-02).</summary>
    /// <response code="200">La lista, en el orden del catálogo.</response>
    [HttpGet("niveles-caloricos")]
    [ProducesResponseType<IReadOnlyList<NomencladorDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<NomencladorDto>> ListarNivelesCaloricos(CancellationToken ct)
        => despachador.EnviarAsync(new ListarNivelesCaloricosQuery(), ct);

    // ---- Grupos nutricionales ----

    /// <summary>Lista completa de los grupos nutricionales.</summary>
    /// <response code="200">La lista, ordenada por nombre.</response>
    [HttpGet("grupos-nutricionales")]
    [ProducesResponseType<IReadOnlyList<NomencladorDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<NomencladorDto>> ListarGruposNutricionales(CancellationToken ct)
        => despachador.EnviarAsync(new ListarGruposNutricionalesQuery(), ct);

    /// <summary>Registra un grupo nutricional.</summary>
    /// <response code="201">Creado.</response>
    /// <response code="409">Ya existe un grupo con ese nombre (RD-08).</response>
    /// <response code="422">La entrada no es válida.</response>
    [Authorize(Policy = Politicas.SoloAdmin)]
    [HttpPost("grupos-nutricionales")]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IdCreadoDto>> RegistrarGrupoNutricional(RegistrarGrupoNutricionalCommand comando, CancellationToken ct)
    {
        var creado = await despachador.EnviarAsync(comando, ct);
        return Created($"/api/nomencladores/grupos-nutricionales/{creado.Id}", creado);
    }

    /// <summary>Elimina un grupo nutricional que no está en uso.</summary>
    /// <response code="204">Eliminado.</response>
    /// <response code="404">No existe.</response>
    /// <response code="409">Está en uso.</response>
    [Authorize(Policy = Politicas.SoloAdmin)]
    [HttpDelete("grupos-nutricionales/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminarGrupoNutricional(Guid id, CancellationToken ct)
    {
        await despachador.EnviarAsync(new EliminarGrupoNutricionalCommand(id), ct);
        return NoContent();
    }

    // ---- Restricciones alimentarias ----

    /// <summary>Lista completa de las restricciones alimentarias.</summary>
    /// <response code="200">La lista, ordenada por nombre.</response>
    [HttpGet("restricciones-alimentarias")]
    [ProducesResponseType<IReadOnlyList<NomencladorDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<NomencladorDto>> ListarRestriccionesAlimentarias(CancellationToken ct)
        => despachador.EnviarAsync(new ListarRestriccionesAlimentariasQuery(), ct);

    /// <summary>Registra una restricción alimentaria.</summary>
    /// <response code="201">Creada.</response>
    /// <response code="409">Ya existe una restricción con ese nombre (RD-08).</response>
    /// <response code="422">La entrada no es válida.</response>
    [Authorize(Policy = Politicas.SoloAdmin)]
    [HttpPost("restricciones-alimentarias")]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IdCreadoDto>> RegistrarRestriccionAlimentaria(
        RegistrarRestriccionAlimentariaCommand comando, CancellationToken ct)
    {
        var creada = await despachador.EnviarAsync(comando, ct);
        return Created($"/api/nomencladores/restricciones-alimentarias/{creada.Id}", creada);
    }

    /// <summary>Elimina una restricción alimentaria que no está en uso.</summary>
    /// <response code="204">Eliminada.</response>
    /// <response code="404">No existe.</response>
    /// <response code="409">Está en uso.</response>
    [Authorize(Policy = Politicas.SoloAdmin)]
    [HttpDelete("restricciones-alimentarias/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminarRestriccionAlimentaria(Guid id, CancellationToken ct)
    {
        await despachador.EnviarAsync(new EliminarRestriccionAlimentariaCommand(id), ct);
        return NoContent();
    }

    // ---- Programas de atención ----

    /// <summary>Lista completa de los programas de atención.</summary>
    /// <response code="200">La lista, ordenada por nombre.</response>
    [HttpGet("programas-atencion")]
    [ProducesResponseType<IReadOnlyList<NomencladorDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<NomencladorDto>> ListarProgramasAtencion(CancellationToken ct)
        => despachador.EnviarAsync(new ListarProgramasAtencionQuery(), ct);

    /// <summary>Registra un programa de atención.</summary>
    /// <response code="201">Creado.</response>
    /// <response code="409">Ya existe un programa con ese nombre (RD-08).</response>
    /// <response code="422">La entrada no es válida.</response>
    [Authorize(Policy = Politicas.SoloAdmin)]
    [HttpPost("programas-atencion")]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IdCreadoDto>> RegistrarProgramaAtencion(RegistrarProgramaAtencionCommand comando, CancellationToken ct)
    {
        var creado = await despachador.EnviarAsync(comando, ct);
        return Created($"/api/nomencladores/programas-atencion/{creado.Id}", creado);
    }

    /// <summary>Elimina un programa de atención que no está en uso.</summary>
    /// <response code="204">Eliminado.</response>
    /// <response code="404">No existe.</response>
    /// <response code="409">Tiene dietas (RR-12).</response>
    [Authorize(Policy = Politicas.SoloAdmin)]
    [HttpDelete("programas-atencion/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminarProgramaAtencion(Guid id, CancellationToken ct)
    {
        await despachador.EnviarAsync(new EliminarProgramaAtencionCommand(id), ct);
        return NoContent();
    }

    // ---- Especialidades ----

    /// <summary>Lista completa de las especialidades.</summary>
    /// <response code="200">La lista, ordenada por nombre.</response>
    [HttpGet("especialidades")]
    [ProducesResponseType<IReadOnlyList<NomencladorDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<NomencladorDto>> ListarEspecialidades(CancellationToken ct)
        => despachador.EnviarAsync(new ListarEspecialidadesQuery(), ct);

    /// <summary>Registra una especialidad.</summary>
    /// <response code="201">Creada.</response>
    /// <response code="409">Ya existe una especialidad con ese nombre (RD-08).</response>
    /// <response code="422">La entrada no es válida.</response>
    [Authorize(Policy = Politicas.SoloAdmin)]
    [HttpPost("especialidades")]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IdCreadoDto>> RegistrarEspecialidad(RegistrarEspecialidadCommand comando, CancellationToken ct)
    {
        var creada = await despachador.EnviarAsync(comando, ct);
        return Created($"/api/nomencladores/especialidades/{creada.Id}", creada);
    }

    /// <summary>Elimina una especialidad que no está en uso.</summary>
    /// <response code="204">Eliminada.</response>
    /// <response code="404">No existe.</response>
    /// <response code="409">Tiene nutricionistas (RR-12).</response>
    [Authorize(Policy = Politicas.SoloAdmin)]
    [HttpDelete("especialidades/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminarEspecialidad(Guid id, CancellationToken ct)
    {
        await despachador.EnviarAsync(new EliminarEspecialidadCommand(id), ct);
        return NoContent();
    }
}
