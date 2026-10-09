namespace Menus.Domain.Abstractions;

/// <summary>
/// Clase base para los agregados raíz del dominio.
/// Un agregado raíz es el punto de entrada al clúster de entidades que lo forman:
/// toda modificación al agregado pasa por esta clase, garantizando consistencia interna.
/// Extiende <see cref="Entity{TId}"/> e incorpora la identidad como parte del contrato.
/// </summary>
/// <typeparam name="TId">Tipo del identificador único del agregado (p. ej. <see cref="Guid"/>).</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
{
    /// <param name="id">Identificador único del agregado.</param>
    protected AggregateRoot(TId id) : base(id) { }
}
