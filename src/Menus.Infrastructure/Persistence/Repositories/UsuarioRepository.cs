using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Implementación de <see cref="IUsuarioRepository"/> sobre EF Core.</summary>
public sealed class UsuarioRepository : Repository<Usuario, Guid>, IUsuarioRepository
{
    /// <param name="context">Contexto de persistencia.</param>
    public UsuarioRepository(MenusDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Usuario?> GetByEmailAsync(string emailNormalizado, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailNormalizado);
        return await Set
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == emailNormalizado, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExisteEmailAsync(string emailNormalizado, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailNormalizado);
        return await Set
            .AsNoTracking()
            .AnyAsync(u => u.Email == emailNormalizado, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Set
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}
