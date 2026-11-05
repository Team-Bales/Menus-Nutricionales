using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Borra un alimento o un plato de PostgreSQL y, si el borrado se confirma, su ficha nutricional en MongoDB
/// (RM-09).
/// </summary>
/// <remarks>
/// <para>
/// <b>Regla de orden (documento 2, §3):</b> primero PostgreSQL, después MongoDB. Un fallo en el primer paso no
/// llega a tocar MongoDB; un fallo en el segundo no deshace el primero ni se propaga como error — deja una
/// ficha huérfana que la conciliación (RM-10) elimina más adelante.
/// </para>
/// <para>
/// No participa de <see cref="Menus.Domain.Abstractions.IUnitOfWork"/>: la escritura a MongoDB es una operación
/// independiente de la transacción relacional, como ese contrato ya documenta.
/// </para>
/// </remarks>
/// <param name="contexto">El contexto de PostgreSQL.</param>
/// <param name="fichas">El almacén de fichas de MongoDB.</param>
/// <param name="logger">El registro de la operación.</param>
public sealed class BajaCoordinadaDeItems(
    MenusDbContext contexto,
    IFichaNutricionalStore fichas,
    ILogger<BajaCoordinadaDeItems> logger)
{
    /// <summary>
    /// Elimina el ítem <paramref name="itemId"/> de PostgreSQL y su ficha nutricional en MongoDB, si tiene una.
    /// </summary>
    /// <param name="itemId">El identificador del alimento o plato a eliminar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// <see cref="ResultadoBaja.NoExiste"/> si no había un ítem con ese identificador (no se toca MongoDB);
    /// <see cref="ResultadoBaja.Eliminado"/> si se borraron los dos (o el ítem no tenía ficha);
    /// <see cref="ResultadoBaja.FichaPendiente"/> si PostgreSQL se borró pero MongoDB falló.
    /// </returns>
    /// <exception cref="Exception">
    /// Si PostgreSQL rechaza el borrado (por ejemplo, un alimento que es ingrediente de un plato, RR-04): la fila
    /// y la ficha, si tenía una, quedan intactas.
    /// </exception>
    public async Task<ResultadoBaja> EliminarAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var clase = await contexto.ItemsAlimentarios
            .AsNoTracking()
            .Where(i => i.Id == itemId)
            .Select(i => EF.Property<string>(i, "Clase"))
            .FirstOrDefaultAsync(cancellationToken);

        if (clase is null)
        {
            return ResultadoBaja.NoExiste;
        }

        await contexto.ItemsAlimentarios
            .Where(i => i.Id == itemId)
            .ExecuteDeleteAsync(cancellationToken);

        try
        {
            await fichas.EliminarPorOrigenAsync(clase, itemId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "No se pudo eliminar la ficha nutricional del ítem {ItemId} ({Clase}); queda pendiente de conciliación (RM-10).",
                itemId,
                clase);
            return ResultadoBaja.FichaPendiente;
        }

        return ResultadoBaja.Eliminado;
    }
}
