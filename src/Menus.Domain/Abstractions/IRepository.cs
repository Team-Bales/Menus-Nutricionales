namespace Menus.Domain.Abstractions;

/// <summary>
/// Operaciones base compartidas por los repositorios de entidades del dominio.
/// Las implementaciones concretas (Infraestructura) deciden el motor de
/// persistencia — este contrato no lo expone.
/// </summary>
/// <typeparam name="TEntity">Entidad del dominio.</typeparam>
/// <typeparam name="TId">Tipo de su identificador.</typeparam>
public interface IRepository<TEntity, TId> where TEntity : class
{
    /// <summary>Busca la entidad por su identificador.</summary>
    /// <param name="id">Identificador de la entidad.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La entidad, o <see langword="null"/> si no existe.</returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>Devuelve todas las entidades del tipo, sin paginar.</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega la entidad. No se guarda hasta que el caso de uso confirma con <see cref="IUnitOfWork.CommitAsync"/>.
    /// </summary>
    /// <param name="entity">Entidad nueva.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca la entidad para borrarla. No se borra hasta que el caso de uso confirma con
    /// <see cref="IUnitOfWork.CommitAsync"/>, y la base puede impedirlo si otras filas la referencian (RESTRICT).
    /// </summary>
    /// <param name="entity">Entidad que se borra.</param>
    void Remove(TEntity entity);
}
