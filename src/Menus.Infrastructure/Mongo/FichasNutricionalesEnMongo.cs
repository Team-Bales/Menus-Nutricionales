using Menus.Application.Catalogo;
using Menus.Domain.Entidades;
using Microsoft.Extensions.Logging;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Implementación de <see cref="IFichasNutricionales"/> sobre <see cref="IFichaNutricionalStore"/> y
/// <see cref="IDesgloseDeIngredientes"/>, cuando hay MongoDB configurado.
/// </summary>
/// <remarks>
/// Cada operación corre después de que el caso de uso confirmó en PostgreSQL. Si MongoDB falla, se registra una
/// advertencia y no se propaga, igual que en <see cref="BajaCoordinadaDeItems"/>: el cambio relacional ya está hecho,
/// y lo que queda (una ficha huérfana, o un desglose que difiere de la composición) es recuperable.
/// </remarks>
/// <param name="fichas">El almacén de fichas, para el borrado.</param>
/// <param name="desglose">El desglose de ingredientes de las fichas de los platos.</param>
/// <param name="logger">El registro de los fallos.</param>
public sealed class FichasNutricionalesEnMongo(
    IFichaNutricionalStore fichas,
    IDesgloseDeIngredientes desglose,
    ILogger<FichasNutricionalesEnMongo> logger) : IFichasNutricionales
{
    /// <inheritdoc/>
    public Task EliminarAsync(ItemAlimentario item)
    {
        var tabla = item is Plato ? "Plato" : "Alimento";
        return IntentarAsync("eliminar la ficha (RM-09)", item.Id, () => fichas.EliminarPorOrigenAsync(tabla, item.Id));
    }

    /// <inheritdoc/>
    public Task AgregarIngredienteAsync(Guid platoId, Alimento alimento)
        => IntentarAsync(
            "agregar un ingrediente al desglose (R8-06)",
            platoId,
            () => desglose.AgregarIngredienteAsync(platoId, alimento.Id, alimento.Nombre));

    /// <inheritdoc/>
    public Task QuitarIngredienteAsync(Guid platoId, Guid alimentoId)
        => IntentarAsync(
            "quitar un ingrediente del desglose (R8-06)",
            platoId,
            () => desglose.QuitarIngredienteAsync(platoId, alimentoId));

    private async Task IntentarAsync(string operacion, Guid itemId, Func<Task<bool>> accion)
    {
        try
        {
            await accion();
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "No se pudo {Operacion} en la ficha nutricional del ítem {ItemId}; queda pendiente de conciliación.",
                operacion,
                itemId);
        }
    }
}
