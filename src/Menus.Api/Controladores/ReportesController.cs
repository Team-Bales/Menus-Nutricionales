using Menus.Api.Auth;
using Menus.Application.Comun;
using Menus.Application.Reportes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Controladores;

/// <summary>
/// Endpoints de reportes nutricionales (consultas informacionales 1-6 del informe §2.2).
/// Acceso restringido a nutricionistas y administradores.
/// </summary>
[ApiController]
[Route("api/reportes")]
[Authorize(Policy = Politicas.NutricionistaOAdmin)]
[Produces("application/json")]
public sealed class ReportesController(IDespachador despachador) : ControllerBase
{
    // ------------------------------------------------------------
    // C1: GET /menus-automaticos
    // ------------------------------------------------------------
    /// <summary>
    /// Lista los menús generados automáticamente de una dieta, con su creador, fecha y parámetros.
    /// </summary>
    /// <param name="dietaId">Identificador de la dieta (requerido).</param>
    /// <param name="orden">Columna de ordenamiento (menuId, creador, fechaCreacion, cantidadTotalAlimentos, distribucion, gruposCubiertos).</param>
    /// <param name="direccion">Dirección del ordenamiento: asc o desc (por defecto asc).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">Lista de menús automáticos.</response>
    /// <response code="401">Sin autenticación.</response>
    /// <response code="403">El rol no tiene permiso (solo Nutricionista/Admin).</response>
    /// <response code="404">La dieta no existe.</response>
    /// <response code="422">Parámetros de ordenamiento inválidos.</response>
    [HttpGet("menus-automaticos")]
    [ProducesResponseType<IReadOnlyList<MenuAutomaticoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<IReadOnlyList<MenuAutomaticoDto>> GetMenusAutomaticos(
        Guid dietaId,
        string? orden = null,
        string? direccion = null,
        CancellationToken ct = default)
    {
        var ordenReporte = string.IsNullOrWhiteSpace(orden) ? null : new OrdenDeReporte(orden, direccion);
        return despachador.EnviarAsync(new ListarMenusAutomaticosQuery(dietaId, ordenReporte), ct);
    }

    // ------------------------------------------------------------
    // C2: GET /alimentos-mas-usados
    // ------------------------------------------------------------
    /// <summary>
    /// Devuelve los alimentos simples más utilizados en menús finales de una dieta, con top-N opcional.
    /// </summary>
    /// <param name="dietaId">Identificador de la dieta (requerido).</param>
    /// <param name="top">Cantidad máxima de alimentos a devolver (1-20, por defecto 10).</param>
    /// <param name="orden">Columna de ordenamiento (alimentoId, alimento, nivelCalorico, grupoNutricional, menusFinales).</param>
    /// <param name="direccion">Dirección del ordenamiento: asc o desc (por defecto asc).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">Lista de alimentos más usados (ya limitada por top en la función SQL).</response>
    /// <response code="401">Sin autenticación.</response>
    /// <response code="403">El rol no tiene permiso (solo Nutricionista/Admin).</response>
    /// <response code="404">La dieta no existe.</response>
    /// <response code="422">Parámetros inválidos (top fuera de rango u ordenamiento inválido).</response>
    [HttpGet("alimentos-mas-usados")]
    [ProducesResponseType<IReadOnlyList<AlimentoMasUsadoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<IReadOnlyList<AlimentoMasUsadoDto>> GetAlimentosMasUsados(
        Guid dietaId,
        int top = 10,
        string? orden = null,
        string? direccion = null,
        CancellationToken ct = default)
    {
        var ordenReporte = string.IsNullOrWhiteSpace(orden) ? null : new OrdenDeReporte(orden, direccion);
        return despachador.EnviarAsync(new ListarAlimentosMasUsadosQuery(dietaId, top, ordenReporte), ct);
    }

    // ------------------------------------------------------------
    // C3: GET /dictamenes
    // ------------------------------------------------------------
    /// <summary>
    /// Devuelve los dictámenes (aprobaciones y rechazos) registrados por un revisor.
    /// El reporte es histórico: no exige que el revisor siga siendo jefe.
    /// </summary>
    /// <param name="revisorId">Identificador del nutricionista revisor (requerido).</param>
    /// <param name="orden">Columna de ordenamiento (menuId, dieta, creador, fechaCreacion, fechaValidacion, aprobado, observaciones, menuReemplazoId).</param>
    /// <param name="direccion">Dirección del ordenamiento: asc o desc (por defecto asc).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">Lista de dictámenes.</response>
    /// <response code="401">Sin autenticación.</response>
    /// <response code="403">El rol no tiene permiso (solo Nutricionista/Admin).</response>
    /// <response code="404">El revisor no existe.</response>
    /// <response code="422">Parámetros de ordenamiento inválidos.</response>
    [HttpGet("dictamenes")]
    [ProducesResponseType<IReadOnlyList<DictamenDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<IReadOnlyList<DictamenDto>> GetDictamenes(
        Guid revisorId,
        string? orden = null,
        string? direccion = null,
        CancellationToken ct = default)
    {
        var ordenReporte = string.IsNullOrWhiteSpace(orden) ? null : new OrdenDeReporte(orden, direccion);
        return despachador.EnviarAsync(new ListarDictamenesQuery(revisorId, ordenReporte), ct);
    }

    // ------------------------------------------------------------
    // C4: GET /desempeno-menu
    // ------------------------------------------------------------
    /// <summary>
    /// Devuelve el desempeño de los pacientes ante un menú, por ítem y nivel calórico.
    /// </summary>
    /// <param name="menuId">Identificador del menú (requerido).</param>
    /// <param name="orden">Columna de ordenamiento (itemId, item, clase, nivelCalorico, serviciosConConsumo, aceptaciones, rechazos, tasaAceptacion).</param>
    /// <param name="direccion">Dirección del ordenamiento: asc o desc (por defecto asc).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">Lista de desempeño por ítem.</response>
    /// <response code="401">Sin autenticación.</response>
    /// <response code="403">El rol no tiene permiso (solo Nutricionista/Admin).</response>
    /// <response code="404">El menú no existe.</response>
    /// <response code="422">Parámetros de ordenamiento inválidos.</response>
    [HttpGet("desempeno-menu")]
    [ProducesResponseType<IReadOnlyList<DesempenoItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<IReadOnlyList<DesempenoItemDto>> GetDesempenoMenu(
        Guid menuId,
        string? orden = null,
        string? direccion = null,
        CancellationToken ct = default)
    {
        var ordenReporte = string.IsNullOrWhiteSpace(orden) ? null : new OrdenDeReporte(orden, direccion);
        return despachador.EnviarAsync(new ListarDesempenoMenuQuery(menuId, ordenReporte), ct);
    }

    // ------------------------------------------------------------
    // C5: GET /comparacion-dietas
    // ------------------------------------------------------------
    /// <summary>
    /// Compara dos o más dietas en tres dimensiones: distribución por grupo, por nivel y criterios de equilibrio.
    /// </summary>
    /// <param name="dietas">Identificadores de las dietas a comparar (mínimo 2, repetir parámetro para cada una).</param>
    /// <param name="soloFinales">Si true, solo menús aprobados; si false, todos los menús (por defecto false).</param>
    /// <param name="ordenGrupos">Columna de ordenamiento para distribución por grupo (dietaId, dieta, grupoNutricional, items, porcentaje).</param>
    /// <param name="direccionGrupos">Dirección para grupos: asc o desc (por defecto asc).</param>
    /// <param name="ordenNiveles">Columna de ordenamiento para distribución por nivel (dietaId, dieta, nivelCalorico, items, porcentaje).</param>
    /// <param name="direccionNiveles">Dirección para niveles: asc o desc (por defecto asc).</param>
    /// <param name="ordenEquilibrio">Columna de ordenamiento para criterios de equilibrio (dietaId, dieta, menuId, estado, items, cantidadDeclarada, cantidadCumple, proporcionesCumplen, tiposFueraDeProporcion, requeridosCubiertos, requeridosFaltantes, prohibidosAusentes, prohibidosPresentes).</param>
    /// <param name="direccionEquilibrio">Dirección para equilibrio: asc o desc (por defecto asc).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">Tres listas: porGrupo, porNivel, criterios.</response>
    /// <response code="401">Sin autenticación.</response>
    /// <response code="403">El rol no tiene permiso (solo Nutricionista/Admin).</response>
    /// <response code="404">Alguna de las dietas no existe.</response>
    /// <response code="422">Parámetros inválidos (menos de 2 dietas, dietas duplicadas, ordenamiento inválido).</response>
    [HttpGet("comparacion-dietas")]
    [ProducesResponseType<ComparacionDietasDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<ComparacionDietasDto> GetComparacionDietas(
        [FromQuery(Name = "dietas")] Guid[] dietas,
        bool soloFinales = false,
        string? ordenGrupos = null,
        string? direccionGrupos = null,
        string? ordenNiveles = null,
        string? direccionNiveles = null,
        string? ordenEquilibrio = null,
        string? direccionEquilibrio = null,
        CancellationToken ct = default)
    {
        var ordenG = string.IsNullOrWhiteSpace(ordenGrupos) ? null : new OrdenDeReporte(ordenGrupos, direccionGrupos);
        var ordenN = string.IsNullOrWhiteSpace(ordenNiveles) ? null : new OrdenDeReporte(ordenNiveles, direccionNiveles);
        var ordenE = string.IsNullOrWhiteSpace(ordenEquilibrio) ? null : new OrdenDeReporte(ordenEquilibrio, direccionEquilibrio);
        return despachador.EnviarAsync(
            new CompararDietasQuery(dietas, soloFinales, ordenG, ordenN, ordenE), ct);
    }

    // ------------------------------------------------------------
    // C6: GET /correlacion-nutricional
    // ------------------------------------------------------------
    /// <summary>
    /// Devuelve la correlación nutricional en tres partes: Spearman por dieta, top-10 ítems rechazados y revalorizaciones.
    /// </summary>
    /// <param name="desde">Inicio del período (inclusivo), formato ISO 8601.</param>
    /// <param name="hasta">Fin del período (exclusivo), formato ISO 8601.</param>
    /// <param name="minimoConsumos">Mínimo de consumos para que un ítem entre en el top-10 de rechazados (por defecto 1, mínimo 1).</param>
    /// <param name="ordenCorrelacion">Columna para correlación (dietaId, dieta, menusValorados, coeficienteSpearman).</param>
    /// <param name="direccionCorrelacion">Dirección para correlación: asc o desc (por defecto asc).</param>
    /// <param name="ordenRechazos">Columna para rechazados (itemId, item, clase, descriptor, consumos, rechazos, tasaRechazo, dietas).</param>
    /// <param name="direccionRechazos">Dirección para rechazados: asc o desc (por defecto asc).</param>
    /// <param name="ordenRevalorizaciones">Columna para revalorizaciones (revalorizacionId, pacienteId, paciente, sala, dieta, valorRevalorizacion, promedioSala, diferencia).</param>
    /// <param name="direccionRevalorizaciones">Dirección para revalorizaciones: asc o desc (por defecto asc).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">Tres listas: correlaciones, itemsMasRechazados, revalorizaciones.</response>
    /// <response code="401">Sin autenticación.</response>
    /// <response code="403">El rol no tiene permiso (solo Nutricionista/Admin).</response>
    /// <response code="422">Parámetros inválidos (desde >= hasta, minimoConsumos &lt; 1, ordenamiento inválido).</response>
    [HttpGet("correlacion-nutricional")]
    [ProducesResponseType<CorrelacionNutricionalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<CorrelacionNutricionalDto> GetCorrelacionNutricional(
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        int minimoConsumos = 1,
        string? ordenCorrelacion = null,
        string? direccionCorrelacion = null,
        string? ordenRechazos = null,
        string? direccionRechazos = null,
        string? ordenRevalorizaciones = null,
        string? direccionRevalorizaciones = null,
        CancellationToken ct = default)
    {
        var ordenC = string.IsNullOrWhiteSpace(ordenCorrelacion) ? null : new OrdenDeReporte(ordenCorrelacion, direccionCorrelacion);
        var ordenR = string.IsNullOrWhiteSpace(ordenRechazos) ? null : new OrdenDeReporte(ordenRechazos, direccionRechazos);
        var ordenV = string.IsNullOrWhiteSpace(ordenRevalorizaciones) ? null : new OrdenDeReporte(ordenRevalorizaciones, direccionRevalorizaciones);
        return despachador.EnviarAsync(
            new ObtenerCorrelacionNutricionalQuery(desde, hasta, minimoConsumos, ordenC, ordenR, ordenV), ct);
    }
}
