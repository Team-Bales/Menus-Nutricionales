using Menus.Application.Comun;
using Menus.Application.GestionDeMenus;
using Menus.Application.GestionDeMenus.ListarMenus;
using Menus.Application.GestionDeMenus.ObtenerMenu;
using Menus.Application.GestionDeMenus.RegistrarReemplazo;
using Menus.Application.GestionDeMenus.ValidarMenu;
using Menus.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Controladores;

/// <summary>
/// Menús: listado por dieta, detalle, dictamen y menú de reemplazo (#80). Cada acción arma el comando o la consulta y
/// lo envía al despachador (ADR-010, §8); los errores los traduce el manejador global.
/// </summary>
/// <remarks>
/// La generación automática está en <c>POST /api/dietas/{dietaId}/menus/generacion-automatica</c> (#98). Los roles de
/// cada acción los pone el #74; mientras tanto, el dictamen ya exige que el actor sea jefe de nutrición autorizado
/// para la dieta (RS-03).
/// </remarks>
[ApiController]
[Route("api/menus")]
[Produces("application/json")]
public sealed class MenusController(IDespachador despachador) : ControllerBase
{
    /// <summary>
    /// Lista por páginas los menús de una dieta, del más reciente al más antiguo, los haya creado cualquiera de los
    /// nutricionistas autorizados para ella.
    /// </summary>
    /// <param name="dietaId">Dieta de los menús. Es obligatoria.</param>
    /// <param name="estado">Solo los menús en ese estado: <c>Pendiente</c>, <c>Final</c> o <c>Rechazado</c>.</param>
    /// <param name="page">Página, desde 1.</param>
    /// <param name="size">Menús por página, de 1 a 100.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">La página, en el sobre <c>{ data, total, page, size }</c>.</response>
    /// <response code="404">La dieta no existe.</response>
    /// <response code="422">Falta la dieta, o la paginación no es válida.</response>
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<MenuDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<ResultadoPaginado<MenuDto>> Listar(
        Guid dietaId,
        EstadoDeMenu? estado,
        CancellationToken ct,
        int page = 1,
        int size = 20)
        => despachador.EnviarAsync(new ListarMenusQuery(dietaId, estado, page, size), ct);

    /// <summary>Detalle de un menú, con su parametrización, sus ítems y su dictamen, todo con nombre.</summary>
    /// <response code="200">El menú.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<MenuDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<MenuDetalleDto> Obtener(Guid id, CancellationToken ct)
        => despachador.EnviarAsync(new ObtenerMenuQuery(id), ct);

    /// <summary>
    /// Registra el dictamen del jefe de nutrición: aprueba el menú, que pasa a ser final, o lo rechaza, y queda
    /// registrado como rechazado.
    /// </summary>
    /// <response code="204">Dictaminado.</response>
    /// <response code="404">El menú no existe.</response>
    /// <response code="409">
    /// El actor no es jefe de nutrición autorizado para la dieta (RS-03), es quien creó el menú (RS-05), o el menú ya
    /// tiene dictamen (RS-15).
    /// </response>
    [HttpPost("{id:guid}/validar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Validar(Guid id, DictamenDeMenu cuerpo, CancellationToken ct)
    {
        await despachador.EnviarAsync(new ValidarMenuCommand(id, cuerpo.Aprobado, cuerpo.Observaciones), ct);
        return NoContent();
    }

    /// <summary>Registra que el menú se confeccionó en lugar de uno rechazado (relación <c>Origina</c>).</summary>
    /// <response code="204">Registrado.</response>
    /// <response code="404">Alguno de los dos menús no existe.</response>
    /// <response code="409">
    /// El otro menú no fue rechazado (RS-17) o es de otra dieta (RS-18), este ya tiene dictamen o ya reemplaza a otro,
    /// o el rechazo ya originó otro menú (RK-15).
    /// </response>
    /// <response code="422">Falta el menú rechazado.</response>
    [HttpPost("{id:guid}/reemplazo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegistrarReemplazo(Guid id, MenuReemplazado cuerpo, CancellationToken ct)
    {
        await despachador.EnviarAsync(new RegistrarReemplazoCommand(id, cuerpo.MenuRechazadoId), ct);
        return NoContent();
    }
}
