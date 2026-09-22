namespace Menus.Domain.Abstractions;

/// <summary>
/// Operaciones base compartidas por los repositorios de entidades del dominio.
/// Las implementaciones concretas (Infraestructura) deciden el motor de
/// persistencia — este contrato no lo expone.
/// </summary>
public interface IRepository<TEntity, TId> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Remove(TEntity entity);
}
