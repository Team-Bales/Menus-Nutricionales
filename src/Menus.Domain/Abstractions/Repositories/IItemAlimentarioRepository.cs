using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="ItemAlimentario"/>: las operaciones que abarcan a alimentos y platos a la vez.
/// Las consultas propias de cada clase están en <see cref="IAlimentoRepository"/> e <see cref="IPlatoRepository"/>.
/// </summary>
public interface IItemAlimentarioRepository : IRepository<ItemAlimentario, Guid>
{
    /// <summary>
    /// Indica si ya existe un alimento o un plato con ese nombre, sin distinguir mayúsculas ni espacios sobrantes
    /// (R5-16: no existen alimentos duplicados).
    /// </summary>
    /// <remarks>
    /// La comprobación abarca a las dos clases porque alimentos y platos son ítems de una misma generalización
    /// (<see cref="ItemAlimentario"/>) y comparten el espacio de nombres: un alimento y un plato no pueden
    /// llamarse igual. Por eso está aquí y no
    /// en los repositorios de cada clase.
    /// </remarks>
    /// <param name="nombre">Nombre que se quiere registrar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default);
}
