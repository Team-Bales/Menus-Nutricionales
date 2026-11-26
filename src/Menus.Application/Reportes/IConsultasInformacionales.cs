using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Menus.Application.Reportes;

/// <summary>
/// Puerto de aplicación para ejecutar las consultas informacionales del informe (§2.2).
/// Cada método invoca una función SQL de PostgreSQL y devuelve las filas en el orden canónico de la función.
/// La capa de aplicación se encarga de validar parámetros, comprobar existencia de recursos y aplicar ordenamiento de presentación.
/// </summary>
public interface IConsultasInformacionales
{
    /// <summary>
    /// Consulta 1: menús generados automáticamente por dieta.
    /// </summary>
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Filas en orden canónico: fecha_creacion DESC, menu_id.</returns>
    Task<IReadOnlyList<MenuAutomaticoDto>> ConsultarMenusAutomaticosAsync(Guid dietaId, CancellationToken ct = default);

    /// <summary>
    /// Consulta 2: alimentos más usados en menús finales de una dieta.
    /// </summary>
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <param name="limite">Límite top-N (null para sin límite).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Filas en orden canónico: menus_finales DESC, alimento.</returns>
    Task<IReadOnlyList<AlimentoMasUsadoDto>> ConsultarAlimentosMasUsadosAsync(Guid dietaId, int? limite, CancellationToken ct = default);

    /// <summary>
    /// Consulta 3: dictámenes de un revisor.
    /// </summary>
    /// <param name="revisorId">Identificador del nutricionista revisor.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Filas en orden canónico: fecha_validacion DESC, menu_id.</returns>
    Task<IReadOnlyList<DictamenDto>> ConsultarDictamenesAsync(Guid revisorId, CancellationToken ct = default);

    /// <summary>
    /// Consulta 4: desempeño ante un menú.
    /// </summary>
    /// <param name="menuId">Identificador del menú.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Filas en orden canónico: nivel_calorico (Bajo/Medio/Alto), tasa_aceptacion DESC NULLS LAST, item.</returns>
    Task<IReadOnlyList<DesempenoItemDto>> ConsultarDesempenoMenuAsync(Guid menuId, CancellationToken ct = default);

    /// <summary>
    /// Consulta 5.1: distribución porcentual por grupo nutricional.
    /// </summary>
    /// <param name="dietas">Identificadores de las dietas a comparar.</param>
    /// <param name="soloFinales">Si true, solo menús aprobados.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Filas en orden canónico: dieta, items DESC, grupo_nutricional.</returns>
    Task<IReadOnlyList<ComparacionPorGrupoDto>> ConsultarComparacionPorGrupoAsync(IReadOnlyList<Guid> dietas, bool soloFinales, CancellationToken ct = default);

    /// <summary>
    /// Consulta 5.2: distribución porcentual por nivel calórico.
    /// </summary>
    /// <param name="dietas">Identificadores de las dietas a comparar.</param>
    /// <param name="soloFinales">Si true, solo menús aprobados.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Filas en orden canónico: dieta, nivel_calorico (Bajo/Medio/Alto).</returns>
    Task<IReadOnlyList<ComparacionPorNivelDto>> ConsultarComparacionPorNivelAsync(IReadOnlyList<Guid> dietas, bool soloFinales, CancellationToken ct = default);

    /// <summary>
    /// Consulta 5.3: criterios de equilibrio por menú.
    /// </summary>
    /// <param name="dietas">Identificadores de las dietas a comparar.</param>
    /// <param name="soloFinales">Si true, solo menús aprobados.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Filas en orden canónico: dieta, menu_id.</returns>
    Task<IReadOnlyList<CriteriosEquilibrioDto>> ConsultarCriteriosEquilibrioAsync(IReadOnlyList<Guid> dietas, bool soloFinales, CancellationToken ct = default);

    /// <summary>
    /// Consulta 6A: correlación de Spearman por dieta.
    /// </summary>
    /// <param name="desde">Inicio del período (inclusivo), null para sin límite.</param>
    /// <param name="hasta">Fin del período (exclusivo), null para sin límite.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Filas en orden canónico: dieta.</returns>
    Task<IReadOnlyList<CorrelacionPorDietaDto>> ConsultarCorrelacionPorDietaAsync(DateTimeOffset? desde, DateTimeOffset? hasta, CancellationToken ct = default);

    /// <summary>
    /// Consulta 6B: top-N ítems con mayor tasa de rechazo.
    /// </summary>
    /// <param name="limite">Límite fijo (10).</param>
    /// <param name="minimoConsumos">Mínimo de consumos para entrar en el ranking.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Filas en orden canónico: tasa_rechazo DESC, rechazos DESC, item.</returns>
    Task<IReadOnlyList<ItemMasRechazadoDto>> ConsultarItemsMasRechazadosAsync(int limite, int minimoConsumos, CancellationToken ct = default);

    /// <summary>
    /// Consulta 6C: pacientes revalorizados frente al promedio de su sala.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Filas en orden canónico: sala, dieta, paciente.</returns>
    Task<IReadOnlyList<RevalorizadoDto>> ConsultarRevalorizadosAsync(CancellationToken ct = default);
}
