using Menus.Infrastructure.Mongo;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Conciliador de fichas nutricionales: elimina huérfanas y actualiza copias de nombre desactualizadas (RM-10).
/// </summary>
/// <remarks>
/// Se ejecuta periódicamente como <see cref="ConciliacionHostedService"/> o se invoca a mano.
/// </remarks>
public interface IFichasConciliador
{
    /// <summary>
    /// Ejecuta una pasada completa de conciliación.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Contadores de la operación.</returns>
    Task<ResultadoConciliacion> ConciliarAsync(CancellationToken cancellationToken = default);
}
