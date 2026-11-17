namespace Menus.Domain.Abstractions;

/// <summary>
/// Coordina en una sola transacción relacional las escrituras de varios Repository
/// dentro de un mismo caso de uso (ej. generación de menú, aprobación, cierre de
/// revalorización). Solo cubre el lado relacional (PostgreSQL) — la escritura a
/// MongoDB se trata como una operación independiente, no como parte de esta unidad.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Confirma en una transacción todos los cambios pendientes de los repositorios del caso de uso. Si una
    /// restricción de la base los rechaza, no se guarda ninguno.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Cantidad de filas escritas.</returns>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
