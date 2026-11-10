namespace Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;

/// <summary>Disponibilidad de un nivel calórico para una dieta (informe, «R1»).</summary>
/// <param name="NivelCaloricoId">Identificador del nivel calórico.</param>
/// <param name="NivelCaloricoNombre">Nombre del nivel calórico.</param>
/// <param name="ItemsDelBanco">Ítems del banco compatibles con la dieta en este nivel.</param>
/// <param name="ItemsEnMenusFinales">Ítems en este nivel incluidos en los menús finales de la dieta.</param>
public sealed record DisponibilidadPorNivelDto(
    Guid NivelCaloricoId,
    string NivelCaloricoNombre,
    int ItemsDelBanco,
    int ItemsEnMenusFinales);

/// <summary>
/// Disponibilidad calórica de una dieta (informe, «R1 — Disponibilidad calórica»): por cada nivel calórico,
/// cuántos ítems del banco son compatibles con la dieta y cómo se distribuyen los ítems de sus menús finales.
/// </summary>
/// <param name="DietaId">Identificador de la dieta.</param>
/// <param name="PorNivel">Los tres niveles del catálogo, en su orden (bajo, medio, alto).</param>
public sealed record DisponibilidadCaloricaDto(Guid DietaId, IReadOnlyList<DisponibilidadPorNivelDto> PorNivel);
