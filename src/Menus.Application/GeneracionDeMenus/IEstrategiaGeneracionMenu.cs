using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.ValueObjects;

namespace Menus.Application.GeneracionDeMenus;

/// <summary>
/// Algoritmo de selección de ítems para la generación automática de un menú (patrón <i>Strategy</i>, #98;
/// checklist #96). Intercambiable: el caso de uso no conoce el criterio de selección, solo que respeta los
/// parámetros de generación.
/// </summary>
public interface IEstrategiaGeneracionMenu
{
    /// <summary>
    /// Selecciona los ítems del menú a partir de los disponibles, que ya excluyen los grupos prohibidos de la
    /// dieta (informe, «generación automática no incluye ítems de grupos prohibidos»).
    /// </summary>
    /// <param name="itemsCompatibles">Ítems del banco compatibles con la dieta (ya sin grupos prohibidos).</param>
    /// <param name="distribucion">Proporción de ítems por tipo de preparación que debe respetar el menú.</param>
    /// <param name="cobertura">Grupos nutricionales que el menú debe cubrir.</param>
    /// <param name="cantidadTotal">Cantidad total de ítems del menú.</param>
    /// <returns>Los identificadores de los ítems seleccionados, sin repetir, en la cantidad pedida.</returns>
    /// <exception cref="ReglaDeNegocioException">
    /// Si no hay suficientes ítems compatibles para cumplir la distribución pedida.
    /// </exception>
    IReadOnlyList<Guid> SeleccionarItems(
        IReadOnlyList<ItemAlimentario> itemsCompatibles,
        DistribucionTiposPreparacion distribucion,
        CoberturaGruposNutricionales cobertura,
        CantidadTotalAlimentos cantidadTotal);
}
