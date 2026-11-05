namespace Menus.Infrastructure.Mongo;

/// <summary>Acceso de escritura a la ficha nutricional de un ítem, por su origen en PostgreSQL.</summary>
/// <remarks>
/// Interfaz propia (no solo <see cref="MenusMongoContext"/>) para poder sustituirla en pruebas, por ejemplo para
/// forzar el camino de <see cref="ResultadoBaja.FichaPendiente"/> en <see cref="BajaCoordinadaDeItems"/>.
/// </remarks>
public interface IFichaNutricionalStore
{
    /// <summary>Elimina la ficha de un ítem, si existe.</summary>
    /// <param name="tabla">La clase del ítem en PostgreSQL: <c>Alimento</c> o <c>Plato</c> (<see cref="OrigenFicha.Tabla"/>).</param>
    /// <param name="itemId">El identificador del ítem en PostgreSQL.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns><see langword="true"/> si había una ficha y se borró; <see langword="false"/> si no había ninguna.</returns>
    Task<bool> EliminarPorOrigenAsync(string tabla, Guid itemId, CancellationToken cancellationToken = default);
}
