using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="Usuario"/> para autenticación y autorización.
/// </summary>
public interface IUsuarioRepository : IRepository<Usuario, Guid>
{
    /// <summary>Busca un usuario por su email normalizado.</summary>
    /// <param name="emailNormalizado">Email ya normalizado (lowercase + trim).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario si existe; <see langword="null"/> en caso contrario.</returns>
    Task<Usuario?> GetByEmailAsync(string emailNormalizado, CancellationToken cancellationToken = default);

    /// <summary>Indica si existe un usuario con el email normalizado dado.</summary>
    /// <param name="emailNormalizado">Email ya normalizado (lowercase + trim).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns><see langword="true"/> si existe; <see langword="false"/> en caso contrario.</returns>
    Task<bool> ExisteEmailAsync(string emailNormalizado, CancellationToken cancellationToken = default);

    /// <summary>Busca un usuario por su identificador, incluyendo la entidad asociada si la tiene.</summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario si existe; <see langword="null"/> en caso contrario.</returns>
    new Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
