using System.ComponentModel.DataAnnotations;

namespace Menus.Application.Reportes;

// DTOs de respuesta para los endpoints de reportes.

#region C1: Menús automáticos por dieta

/// <summary>
/// Fila de la consulta 1: menús generados automáticamente de una dieta.
/// </summary>
/// <param name="MenuId">Identificador del menú.</param>
/// <param name="Creador">Nombre del nutricionista creador.</param>
/// <param name="FechaCreacion">Fecha y hora de creación del menú.</param>
/// <param name="CantidadTotalAlimentos">Cantidad total de alimentos declarada para el menú.</param>
/// <param name="Distribucion">Distribución porcentual por tipo de preparación, como texto legible.</param>
/// <param name="GruposCubiertos">Grupos nutricionales cubiertos por el menú, como texto legible.</param>
public sealed record MenuAutomaticoDto(
    Guid MenuId,
    string Creador,
    DateTimeOffset FechaCreacion,
    int CantidadTotalAlimentos,
    string Distribucion,
    string GruposCubiertos);

#endregion

#region C2: Alimentos más usados

/// <summary>
/// Fila de la consulta 2: alimentos simples más utilizados en menús finales de una dieta.
/// </summary>
/// <param name="AlimentoId">Identificador del alimento.</param>
/// <param name="Alimento">Nombre del alimento.</param>
/// <param name="NivelCalorico">Nivel calórico del alimento.</param>
/// <param name="GrupoNutricional">Grupo nutricional del alimento.</param>
/// <param name="MenusFinales">Cantidad de menús finales distintos que lo contienen.</param>
public sealed record AlimentoMasUsadoDto(
    Guid AlimentoId,
    string Alimento,
    string NivelCalorico,
    string GrupoNutricional,
    long MenusFinales);

#endregion

#region C3: Dictámenes de un revisor

/// <summary>
/// Fila de la consulta 3: dictámenes registrados por un revisor.
/// </summary>
/// <param name="MenuId">Identificador del menú evaluado.</param>
/// <param name="Dieta">Nombre de la dieta del menú.</param>
/// <param name="Creador">Nombre del nutricionista creador del menú.</param>
/// <param name="FechaCreacion">Fecha de creación del menú.</param>
/// <param name="FechaValidacion">Fecha del dictamen.</param>
/// <param name="Aprobado">Si el dictamen aprobó el menú.</param>
/// <param name="Observaciones">Observaciones del revisor.</param>
/// <param name="MenuReemplazoId">Identificador del menú de reemplazo, si el dictamen fue un rechazo que originó uno.</param>
public sealed record DictamenDto(
    Guid MenuId,
    string Dieta,
    string Creador,
    DateTimeOffset FechaCreacion,
    DateTimeOffset FechaValidacion,
    bool Aprobado,
    string? Observaciones,
    Guid? MenuReemplazoId);

#endregion

#region C4: Desempeño ante un menú

/// <summary>
/// Fila de la consulta 4: desempeño de los pacientes ante un menú, por ítem.
/// </summary>
/// <param name="ItemId">Identificador del ítem (alimento o plato).</param>
/// <param name="Item">Nombre del ítem.</param>
/// <param name="Clase">Clase del ítem (<c>Alimento</c> o <c>Plato</c>).</param>
/// <param name="NivelCalorico">Nivel calórico del ítem.</param>
/// <param name="ServiciosConConsumo">Servicios en los que se registró consumo de este ítem.</param>
/// <param name="Aceptaciones">Consumos aceptados.</param>
/// <param name="Rechazos">Consumos rechazados.</param>
/// <param name="TasaAceptacion">Proporción de aceptaciones sobre servicios con consumo (null si no hay consumos).</param>
public sealed record DesempenoItemDto(
    Guid ItemId,
    string Item,
    string Clase,
    string NivelCalorico,
    long ServiciosConConsumo,
    long Aceptaciones,
    long Rechazos,
    decimal? TasaAceptacion);

#endregion

#region C5: Comparación entre dietas

/// <summary>
/// Fila de la consulta 5.1: distribución porcentual de ítems por grupo nutricional.
/// </summary>
/// <param name="DietaId">Identificador de la dieta.</param>
/// <param name="Dieta">Nombre de la dieta.</param>
/// <param name="GrupoNutricional">Nombre del grupo nutricional.</param>
/// <param name="Items">Cantidad de ítems incluidos en ese grupo.</param>
/// <param name="Porcentaje">Porcentaje sobre el total de ítems de la dieta.</param>
public sealed record ComparacionPorGrupoDto(
    Guid DietaId,
    string Dieta,
    string GrupoNutricional,
    long Items,
    decimal Porcentaje);

/// <summary>
/// Fila de la consulta 5.2: distribución porcentual de ítems por nivel calórico.
/// </summary>
/// <param name="DietaId">Identificador de la dieta.</param>
/// <param name="Dieta">Nombre de la dieta.</param>
/// <param name="NivelCalorico">Nombre del nivel calórico.</param>
/// <param name="Items">Cantidad de ítems incluidos en ese nivel.</param>
/// <param name="Porcentaje">Porcentaje sobre el total de ítems de la dieta.</param>
public sealed record ComparacionPorNivelDto(
    Guid DietaId,
    string Dieta,
    string NivelCalorico,
    long Items,
    decimal Porcentaje);

/// <summary>
/// Fila de la consulta 5.3: criterios de equilibrio de un menú.
/// </summary>
/// <param name="DietaId">Identificador de la dieta.</param>
/// <param name="Dieta">Nombre de la dieta.</param>
/// <param name="MenuId">Identificador del menú.</param>
/// <param name="Estado">Estado del menú (<c>Pendiente</c>, <c>Final</c>, <c>Rechazado</c>).</param>
/// <param name="Items">Cantidad de ítems incluidos en el menú.</param>
/// <param name="CantidadDeclarada">Cantidad total de alimentos declarada para el menú.</param>
/// <param name="CantidadCumple">Si la cantidad de ítems coincide con la declarada.</param>
/// <param name="ProporcionesCumplen">Si las proporciones por tipo de preparación se cumplen.</param>
/// <param name="TiposFueraDeProporcion">Tipos de preparación que no cumplen la proporción declarada, como texto legible.</param>
/// <param name="RequeridosCubiertos">Si todos los grupos requeridos están cubiertos.</param>
/// <param name="RequeridosFaltantes">Grupos requeridos no cubiertos, como texto legible.</param>
/// <param name="ProhibidosAusentes">Si no hay grupos prohibidos presentes.</param>
/// <param name="ProhibidosPresentes">Grupos prohibidos presentes, como texto legible.</param>
public sealed record CriteriosEquilibrioDto(
    Guid DietaId,
    string Dieta,
    Guid MenuId,
    string Estado,
    long Items,
    int CantidadDeclarada,
    bool CantidadCumple,
    bool ProporcionesCumplen,
    string? TiposFueraDeProporcion,
    bool RequeridosCubiertos,
    string? RequeridosFaltantes,
    bool ProhibidosAusentes,
    string? ProhibidosPresentes);

/// <summary>
/// Respuesta completa de la consulta 5: comparación de dietas en tres dimensiones.
/// </summary>
/// <param name="PorGrupo">Distribución por grupo nutricional.</param>
/// <param name="PorNivel">Distribución por nivel calórico.</param>
/// <param name="Criterios">Criterios de equilibrio por menú.</param>
public sealed record ComparacionDietasDto(
    IReadOnlyList<ComparacionPorGrupoDto> PorGrupo,
    IReadOnlyList<ComparacionPorNivelDto> PorNivel,
    IReadOnlyList<CriteriosEquilibrioDto> Criterios);

#endregion

#region C6: Correlación nutricional

/// <summary>
/// Fila de la consulta 6A: correlación de Spearman por dieta.
/// </summary>
/// <param name="DietaId">Identificador de la dieta.</param>
/// <param name="Dieta">Nombre de la dieta.</param>
/// <param name="MenusValorados">Cantidad de menús con valoraciones en el período.</param>
/// <param name="CoeficienteSpearman">Coeficiente de correlación de rangos de Spearman (null si no es calculable).</param>
public sealed record CorrelacionPorDietaDto(
    Guid DietaId,
    string Dieta,
    long MenusValorados,
    decimal? CoeficienteSpearman);

/// <summary>
/// Fila de la consulta 6B: ítems con mayor tasa de rechazo.
/// </summary>
/// <param name="ItemId">Identificador del ítem.</param>
/// <param name="Item">Nombre del ítem.</param>
/// <param name="Clase">Clase del ítem (<c>Alimento</c> o <c>Plato</c>).</param>
/// <param name="Descriptor">Nombre del nutricionista que describió el ítem.</param>
/// <param name="Consumos">Total de consumos registrados del ítem.</param>
/// <param name="Rechazos">Consumos rechazados.</param>
/// <param name="TasaRechazo">Proporción de rechazos sobre consumos.</param>
/// <param name="Dietas">Dietas de los menús donde el ítem tuvo rechazos, como texto legible.</param>
public sealed record ItemMasRechazadoDto(
    Guid ItemId,
    string Item,
    string Clase,
    string Descriptor,
    long Consumos,
    long Rechazos,
    decimal TasaRechazo,
    string Dietas);

/// <summary>
/// Fila de la consulta 6C: pacientes revalorizados frente al promedio de su sala.
/// </summary>
/// <param name="RevalorizacionId">Identificador de la revalorización.</param>
/// <param name="PacienteId">Identificador del paciente.</param>
/// <param name="Paciente">Nombre del paciente.</param>
/// <param name="Sala">Nombre de la sala del paciente.</param>
/// <param name="Dieta">Nombre de la dieta del menú valorado.</param>
/// <param name="ValorRevalorizacion">Valor de la valoración generada por la revalorización.</param>
/// <param name="PromedioSala">Promedio de valoraciones de los demás pacientes de la sala en la misma dieta (null si no hay).</param>
/// <param name="Diferencia">Diferencia entre el valor de la revalorización y el promedio de la sala (null si no hay promedio).</param>
public sealed record RevalorizadoDto(
    Guid RevalorizacionId,
    Guid PacienteId,
    string Paciente,
    string Sala,
    string Dieta,
    int ValorRevalorizacion,
    decimal? PromedioSala,
    decimal? Diferencia);

/// <summary>
/// Respuesta completa de la consulta 6: correlación nutricional en tres partes.
/// </summary>
/// <param name="Correlaciones">Correlación de Spearman por dieta.</param>
/// <param name="ItemsMasRechazados">Top-10 de ítems con mayor tasa de rechazo.</param>
/// <param name="Revalorizaciones">Pacientes revalorizados frente al promedio de su sala.</param>
public sealed record CorrelacionNutricionalDto(
    IReadOnlyList<CorrelacionPorDietaDto> Correlaciones,
    IReadOnlyList<ItemMasRechazadoDto> ItemsMasRechazados,
    IReadOnlyList<RevalorizadoDto> Revalorizaciones);

#endregion

#region Ordenamiento

/// <summary>
/// Especificación de ordenamiento para un reporte: columna y dirección.
/// </summary>
/// <param name="Columna">Nombre de la columna en camelCase (JSON), o null para orden canónico.</param>
/// <param name="Direccion">Dirección: <c>asc</c> o <c>desc</c> (sin distinguir mayúsculas), o null para <c>asc</c> por defecto.</param>
public sealed record OrdenDeReporte(string? Columna, string? Direccion)
{
    /// <summary>Indica si hay una especificación de ordenamiento explícita.</summary>
    public bool TieneOrden => !string.IsNullOrWhiteSpace(Columna);

    /// <summary>Dirección normalizada a minúsculas (<c>asc</c> o <c>desc</c>), o <c>asc</c> por defecto.</summary>
    public string DireccionNormalizada => string.IsNullOrWhiteSpace(Direccion) ? "asc" : Direccion.Trim().ToLowerInvariant();

    /// <summary>Valida que la dirección sea <c>asc</c> o <c>desc</c> (sin distinguir mayúsculas).</summary>
    public bool DireccionValida => string.IsNullOrWhiteSpace(Direccion) || DireccionNormalizada is "asc" or "desc";
}

#endregion
