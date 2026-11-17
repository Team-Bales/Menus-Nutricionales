using Menus.Application.Comun;

namespace Menus.Application.GeneracionDeMenus.GenerarMenuAutomaticamente;

/// <summary>
/// Genera automáticamente un menú para una dieta (#98; informe, RF-10), aplicando los parámetros de generación
/// como datos inmutables. El nutricionista creador es el actor actual, nunca el cliente; debe estar autorizado
/// para la dieta (RF-06).
/// </summary>
/// <param name="DietaId">Dieta para la que se genera el menú.</param>
/// <param name="CantidadTotalAlimentos">Cantidad total de ítems del menú (&gt; 0).</param>
/// <param name="Distribucion">Proporción de ítems por tipo de preparación; debe sumar 1.</param>
/// <param name="GruposNutricionalesIds">Grupos nutricionales que debe cubrir el menú.</param>
public sealed record GenerarMenuAutomaticamenteCommand(
    Guid DietaId,
    int CantidadTotalAlimentos,
    IReadOnlyList<(Guid TipoPreparacionId, decimal Proporcion)> Distribucion,
    IReadOnlyList<Guid> GruposNutricionalesIds) : ICommand<IdCreadoDto>;
