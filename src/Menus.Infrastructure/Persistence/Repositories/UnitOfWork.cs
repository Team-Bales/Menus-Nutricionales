using Menus.Domain.Abstractions;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Implementación de <see cref="IUnitOfWork"/> sobre <see cref="MenusDbContext.SaveChangesAsync"/>.</summary>
/// <param name="context">Contexto de persistencia.</param>
public sealed class UnitOfWork(MenusDbContext context) : IUnitOfWork
{
    /// <inheritdoc/>
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
