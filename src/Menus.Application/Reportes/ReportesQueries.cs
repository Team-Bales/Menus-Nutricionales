using Menus.Application.Comun;

namespace Menus.Application.Reportes;

// Consultas (IQuery) para los endpoints de reportes.

#region C1: Menús automáticos por dieta

/// <summary>
/// Lista los menús generados automáticamente de una dieta, con su creador, fecha y parámetros.
/// </summary>
/// <param name="DietaId">Identificador de la dieta (requerido).</param>
/// <param name="Orden">Columna y dirección de ordenamiento.</param>
/// <returns>Lista de menús automáticos (sin paginar).</returns>
public sealed record ListarMenusAutomaticosQuery(
    Guid DietaId,
    OrdenDeReporte? Orden = null) : IQuery<IReadOnlyList<MenuAutomaticoDto>>;

#endregion

#region C2: Alimentos más usados

/// <summary>
/// Devuelve los alimentos simples más utilizados en menús finales de una dieta, con top-N opcional.
/// </summary>
/// <param name="DietaId">Identificador de la dieta (requerido).</param>
/// <param name="Top">Cantidad máxima de alimentos a devolver (1-20, por defecto 10).</param>
/// <param name="Orden">Columna y dirección de ordenamiento.</param>
/// <returns>Lista de alimentos más usados (ya limitada por top en la función SQL).</returns>
public sealed record ListarAlimentosMasUsadosQuery(
    Guid DietaId,
    int Top = 10,
    OrdenDeReporte? Orden = null) : IQuery<IReadOnlyList<AlimentoMasUsadoDto>>;

#endregion

#region C3: Dictámenes de un revisor

/// <summary>
/// Devuelve los dictámenes (aprobaciones y rechazos) registrados por un revisor.
/// </summary>
/// <param name="RevisorId">Identificador del nutricionista revisor (requerido).</param>
/// <param name="Orden">Columna y dirección de ordenamiento.</param>
/// <returns>Lista de dictámenes.</returns>
public sealed record ListarDictamenesQuery(
    Guid RevisorId,
    OrdenDeReporte? Orden = null) : IQuery<IReadOnlyList<DictamenDto>>;

#endregion

#region C4: Desempeño ante un menú

/// <summary>
/// Devuelve el desempeño de los pacientes ante un menú, por ítem y nivel calórico.
/// </summary>
/// <param name="MenuId">Identificador del menú (requerido).</param>
/// <param name="Orden">Columna y dirección de ordenamiento.</param>
/// <returns>Lista de desempeño por ítem.</returns>
public sealed record ListarDesempenoMenuQuery(
    Guid MenuId,
    OrdenDeReporte? Orden = null) : IQuery<IReadOnlyList<DesempenoItemDto>>;

#endregion

#region C5: Comparación entre dietas

/// <summary>
/// Compara dos o más dietas en tres dimensiones: distribución por grupo, por nivel y criterios de equilibrio.
/// </summary>
/// <param name="Dietas">Identificadores de las dietas a comparar (mínimo 2).</param>
/// <param name="SoloFinales">Si true, solo menús aprobados; si false, todos los menús (por defecto false).</param>
/// <param name="OrdenGrupos">Ordenamiento para la sección de distribución por grupo.</param>
/// <param name="OrdenNiveles">Ordenamiento para la sección de distribución por nivel.</param>
/// <param name="OrdenEquilibrio">Ordenamiento para la sección de criterios de equilibrio.</param>
/// <returns>Tres listas independientes con sus propios ordenamientos.</returns>
public sealed record CompararDietasQuery(
    IReadOnlyList<Guid> Dietas,
    bool SoloFinales = false,
    OrdenDeReporte? OrdenGrupos = null,
    OrdenDeReporte? OrdenNiveles = null,
    OrdenDeReporte? OrdenEquilibrio = null) : IQuery<ComparacionDietasDto>;

#endregion

#region C6: Correlación nutricional

/// <summary>
/// Devuelve la correlación nutricional en tres partes: Spearman por dieta, top-10 ítems rechazados y revalorizaciones.
/// </summary>
/// <param name="Desde">Inicio del período (inclusivo), null para sin límite inferior.</param>
/// <param name="Hasta">Fin del período (exclusivo), null para sin límite superior.</param>
/// <param name="MinimoConsumos">Mínimo de consumos para que un ítem entre en el top-10 de rechazados (por defecto 1, mínimo 1).</param>
/// <param name="OrdenCorrelacion">Ordenamiento para la sección de correlación de Spearman.</param>
/// <param name="OrdenRechazos">Ordenamiento para la sección de ítems más rechazados.</param>
/// <param name="OrdenRevalorizaciones">Ordenamiento para la sección de revalorizaciones.</param>
/// <returns>Tres listas independientes con sus propios ordenamientos.</returns>
public sealed record ObtenerCorrelacionNutricionalQuery(
    DateTimeOffset? Desde,
    DateTimeOffset? Hasta,
    int MinimoConsumos = 1,
    OrdenDeReporte? OrdenCorrelacion = null,
    OrdenDeReporte? OrdenRechazos = null,
    OrdenDeReporte? OrdenRevalorizaciones = null) : IQuery<CorrelacionNutricionalDto>;

#endregion
