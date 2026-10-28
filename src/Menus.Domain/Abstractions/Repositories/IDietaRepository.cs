using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="Dieta"/>. Además de las operaciones base, expone las consultas que
/// necesitan el catálogo y la gestión de dietas, y la generación de menús (issues #60, #63 y #98).
/// </summary>
public interface IDietaRepository : IRepository<Dieta, Guid>
{
    /// <summary>
    /// Devuelve una dieta con sus grupos nutricionales (<see cref="Dieta.GruposNutricionales"/>, con su
    /// tipo de cobertura) y sus restricciones (<see cref="Dieta.Restricciones"/>) cargados. Es la que se
    /// usa para modificarlas, porque la regla de no repetir un grupo o una restricción (RC-08) se
    /// comprueba sobre esas colecciones.
    /// </summary>
    /// <param name="id">Identificador de la dieta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La dieta, o <see langword="null"/> si no existe.</returns>
    Task<Dieta?> GetConGruposYRestriccionesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Devuelve las dietas de un programa de atención.</summary>
    /// <param name="programaAtencionId">Identificador del programa de atención.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Dieta>> GetByProgramaAtencionAsync(Guid programaAtencionId, CancellationToken cancellationToken = default);
}
