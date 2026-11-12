namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Escritura del desglose de <c>ingredientes</c> de la ficha de un plato, que tiene que coincidir con su composición en
/// PostgreSQL (R8-06).
/// </summary>
/// <remarks>
/// Va aparte de <see cref="IFichaNutricionalStore"/> para no obligar a cambiar las implementaciones de esa interfaz que
/// solo necesitan el borrado (por ejemplo, los almacenes de prueba de <see cref="BajaCoordinadaDeItems"/>).
/// </remarks>
public interface IDesgloseDeIngredientes
{
    /// <summary>
    /// Agrega un alimento al desglose de la ficha de un plato. Solo toca una ficha que ya tiene desglose y que no incluye
    /// ese alimento: no crea un desglose parcial ni repite el alimento.
    /// </summary>
    /// <param name="platoId">El identificador del plato en PostgreSQL.</param>
    /// <param name="alimentoId">El alimento agregado a la composición.</param>
    /// <param name="nombre">El nombre del alimento, que el desglose guarda como copia.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns><see langword="true"/> si se modificó una ficha.</returns>
    Task<bool> AgregarIngredienteAsync(Guid platoId, Guid alimentoId, string nombre, CancellationToken cancellationToken = default);

    /// <summary>Quita un alimento del desglose de la ficha de un plato, si está.</summary>
    /// <param name="platoId">El identificador del plato en PostgreSQL.</param>
    /// <param name="alimentoId">El alimento quitado de la composición.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns><see langword="true"/> si se modificó una ficha.</returns>
    Task<bool> QuitarIngredienteAsync(Guid platoId, Guid alimentoId, CancellationToken cancellationToken = default);
}
