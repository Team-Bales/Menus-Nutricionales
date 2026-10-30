using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="Nutricionista"/>. Además de las operaciones base, expone la consulta
/// de los nutricionistas autorizados en una dieta (relación <c>Autoriza</c>), que necesitan la gestión
/// de nutricionistas y el listado de menús de una dieta (issues #71 y #80).
/// </summary>
/// <remarks>
/// <see cref="IRepository{TEntity, TId}.GetByIdAsync"/> devuelve el nutricionista con sus dietas
/// autorizadas (<see cref="Nutricionista.DietasAutorizadas"/>) cargadas, porque la regla de no repetir
/// una autorización (RC-06) se comprueba sobre esa colección.
/// </remarks>
public interface INutricionistaRepository : IRepository<Nutricionista, Guid>
{
    /// <summary>Devuelve los nutricionistas autorizados en una dieta (relación <c>Autoriza</c>).</summary>
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Nutricionista>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default);
}
