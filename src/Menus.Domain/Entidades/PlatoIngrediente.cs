using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Entidad de unión que representa la composición de un plato por un alimento
/// con una cantidad específica (relación <c>Compone</c>, con su atributo <c>Cantidad</c>).
/// </summary>
/// <remarks>
/// Los dos extremos son ítems del banco (<see cref="ItemAlimentario"/>), pero con papeles fijos: el compuesto es
/// un <see cref="Plato"/> y el componente, un <see cref="Alimento"/> simple. Así la composición tiene un solo
/// nivel (RS-12).
/// </remarks>
public sealed class PlatoIngrediente : Entity<(Guid PlatoId, Guid AlimentoId)>
{
    /// <param name="platoId">Identificador del plato compuesto.</param>
    /// <param name="alimentoId">Identificador del alimento simple que es ingrediente.</param>
    /// <param name="cantidad">Cantidad del alimento en el plato (estrictamente positiva).</param>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="cantidad"/> no es positiva.</exception>
    public PlatoIngrediente(Guid platoId, Guid alimentoId, decimal cantidad)
        : base((platoId, alimentoId))
    {
        if (cantidad <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser estrictamente positiva.");
        }
        Cantidad = cantidad;
    }

    /// <summary>FK → Plato (parte de la clave compuesta).</summary>
    public Guid PlatoId => Id.PlatoId;

    /// <summary>FK → Alimento (parte de la clave compuesta).</summary>
    public Guid AlimentoId => Id.AlimentoId;

    /// <summary>Cantidad del alimento en el plato (> 0).</summary>
    public decimal Cantidad { get; }
}
