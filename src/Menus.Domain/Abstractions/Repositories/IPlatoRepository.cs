using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="Plato"/>. Además de las operaciones base, expone las consultas
/// que necesita el catálogo de platos (issues #60 y #62).
/// </summary>
/// <remarks>
/// Que el nombre no esté repetido se comprueba con <see cref="IItemAlimentarioRepository.ExisteNombreAsync"/>,
/// porque abarca también a los alimentos (R5-16, extendida a los platos por decisión del equipo: submodelo #5;
/// con la generalización <see cref="ItemAlimentario"/>, los dos comparten un mismo espacio de nombres).
/// </remarks>
public interface IPlatoRepository : IRepository<Plato, Guid>
{
    /// <summary>Devuelve un plato con sus ingredientes (<see cref="Plato.Ingredientes"/>) cargados.</summary>
    /// <param name="id">Identificador del plato.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El plato, o <see langword="null"/> si no existe.</returns>
    Task<Plato?> GetConIngredientesAsync(Guid id, CancellationToken cancellationToken = default);
}
