using Menus.Domain.Abstractions;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación base de <see cref="IRepository{TEntity, TId}"/> sobre EF Core.
/// </summary>
/// <typeparam name="TEntity">Entidad del dominio.</typeparam>
/// <typeparam name="TId">Tipo del identificador de la entidad.</typeparam>
public abstract class Repository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <param name="context">Contexto de persistencia.</param>
    protected Repository(MenusDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Context = context;
    }

    /// <summary>Contexto de persistencia compartido por los repositorios.</summary>
    protected MenusDbContext Context { get; }

    /// <summary>Conjunto de entidades de <typeparamref name="TEntity"/>.</summary>
    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    /// <inheritdoc/>
    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        => await Set.FindAsync(new object?[] { id }, cancellationToken);

    /// <inheritdoc/>
    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
        => await Set.AsNoTracking().ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await Set.AddAsync(entity, cancellationToken);

    /// <inheritdoc/>
    public void Remove(TEntity entity) => Set.Remove(entity);
}
