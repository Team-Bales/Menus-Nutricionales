namespace Menus.Domain.Abstractions;

/// <summary>
/// Clase base para todas las entidades del dominio.
/// La igualdad se determina por <typeparamref name="TId"/> (identidad), no por referencia.
/// Dos instancias del mismo tipo con el mismo Id representan el mismo objeto de negocio.
/// </summary>
/// <typeparam name="TId">Tipo del identificador único de la entidad (p. ej. <see cref="Guid"/>).</typeparam>
public abstract class Entity<TId>
{
    /// <param name="id">Identificador único de la entidad. No debe ser nulo.</param>
    protected Entity(TId id) => Id = id;

    /// <summary>Identificador único de la entidad.</summary>
    public TId Id { get; }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Id?.Equals(other.Id) ?? false;
    }

    /// <inheritdoc/>
    public override int GetHashCode() => Id?.GetHashCode() ?? 0;

    /// <summary>Compara dos entidades por identidad.</summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left?.Equals(right) ?? right is null;

    /// <summary>Compara dos entidades por identidad.</summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !(left == right);
}
