using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio del agregado <see cref="Menu"/>. Además de las operaciones base, expone las consultas
/// que necesitan la generación, el listado, la validación y el acceso de los pacientes a sus menús
/// y a su consumo (issues #80, #84, #85, #99 y #100).
/// </summary>
/// <remarks>
/// <see cref="IRepository{TEntity, TId}.GetByIdAsync"/> devuelve el agregado completo, con alimentos,
/// platos, distribución, grupos, validaciones y asignaciones con sus consumos, porque las reglas del
/// agregado (R7-04, R7-05, R7-06, R7-07, R7-25) se comprueban sobre esas colecciones.
/// <see cref="Validacion"/> y <see cref="Consumo"/> no tienen repositorio propio: son agregaciones sin
/// identidad propia (MERX, §0.2) y se guardan junto con el menú.
/// El estado de un menú no se almacena, se deriva de sus validaciones: es final si su validación más
/// reciente está aprobada (submodelo #7, §4; consulta 2, §7-A). El MERX no define «pendiente»; aquí es
/// el menú sin ninguna validación, porque un menú rechazado no vuelve a revisarse: se confecciona otro
/// (submodelo #7, §2.1, convención de R7-04).
/// </remarks>
public interface IMenuRepository : IRepository<Menu, Guid>
{
    /// <summary>Devuelve los menús de una dieta, los haya creado quien los haya creado.</summary>
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Menu>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default);

    /// <summary>Devuelve los menús que creó un nutricionista (relación <c>Crea</c>).</summary>
    /// <param name="nutricionistaId">Identificador del nutricionista creador.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Menu>> GetByNutricionistaCreadorAsync(Guid nutricionistaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve los menús asignados a un paciente, a través de sus asignaciones (agregación <c>Asignación</c>),
    /// con los consumos de cada asignación: de ahí sale el historial de consumo del paciente (issue #85).
    /// </summary>
    /// <param name="pacienteId">Identificador del paciente.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Menu>> GetByPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve los menús finales de una dieta: aquellos cuya validación más reciente (la de mayor
    /// fecha de validación) está aprobada (submodelo #7, §4; consulta 2, §7-A).
    /// </summary>
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Menu>> GetFinalesByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve los menús pendientes de una dieta: los que todavía no tienen ninguna validación registrada.
    /// </summary>
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Menu>> GetPendientesByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default);
}
