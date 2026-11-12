using Menus.Domain.Entidades;

namespace Menus.Application.Catalogo;

/// <summary>
/// Lo que los casos de uso del catálogo mantienen de la ficha nutricional de un ítem, que vive en MongoDB (ADR-005,
/// ADR-009). La implementación está en <c>Menus.Infrastructure</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Se llama después de <c>IUnitOfWork.CommitAsync</c>,</b> nunca antes (ADR-010, §5, y la regla de orden del
/// documento 2 de la ficha, §3): primero PostgreSQL, después MongoDB. Así no se toca una ficha por un cambio que no
/// llegó a confirmarse.
/// </para>
/// <para>
/// <b>Nunca lanza.</b> Para cuando se la llama, el cambio en PostgreSQL ya está confirmado, y un fallo de MongoDB no
/// puede deshacerlo ni convertirlo en un error para el cliente. Se registra y deja un estado recuperable: una ficha
/// huérfana, que elimina la conciliación (RM-10, #205), o un desglose que difiere de la composición.
/// </para>
/// <para>
/// <b>La ficha es opcional:</b> si el ítem no tiene, las operaciones no hacen nada.
/// </para>
/// <para>
/// <b>No reciben token de cancelación</b> por lo mismo: si el cliente corta la petición después de confirmar, la ficha
/// igual tiene que quedar al día con lo confirmado.
/// </para>
/// </remarks>
public interface IFichasNutricionales
{
    /// <summary>Elimina la ficha del alimento o el plato que se acaba de eliminar (RM-09).</summary>
    /// <param name="item">El ítem eliminado.</param>
    Task EliminarAsync(ItemAlimentario item);

    /// <summary>
    /// Agrega el alimento al desglose de ingredientes de la ficha del plato, para que coincida con su composición
    /// (R8-06). Si la ficha no tiene desglose, no lo crea: uno parcial no coincidiría con la composición.
    /// </summary>
    /// <param name="platoId">El plato al que se agregó el ingrediente.</param>
    /// <param name="alimento">El alimento agregado; su nombre se copia en el desglose.</param>
    Task AgregarIngredienteAsync(Guid platoId, Alimento alimento);

    /// <summary>Quita el alimento del desglose de ingredientes de la ficha del plato (R8-06).</summary>
    /// <param name="platoId">El plato del que se quitó el ingrediente.</param>
    /// <param name="alimentoId">El alimento quitado.</param>
    Task QuitarIngredienteAsync(Guid platoId, Guid alimentoId);
}
