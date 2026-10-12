using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio del agregado <see cref="Menu"/>. Además de las operaciones base, expone las consultas
/// que necesitan la generación, el listado, la validación y el acceso de los pacientes a sus menús
/// (issues #80, #84, #99 y #100).
/// </summary>
/// <remarks>
/// <see cref="IRepository{TEntity, TId}.GetByIdAsync"/> devuelve el agregado completo, con alimentos,
/// platos, distribución, grupos y asignaciones, porque las reglas del agregado (R7-05, R7-07) se
/// comprueban sobre esas colecciones.
/// El estado de un menú no se almacena: es final si su validación más reciente está aprobada,
/// y está pendiente mientras no tenga ninguna validación (submodelo #7, §4).
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

    /// <summary>Devuelve los menús asignados a un paciente, a través de sus asignaciones (agregación <c>Asignación</c>).</summary>
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

    /// <summary>Devuelve los menús de una dieta que todavía no tienen ninguna validación registrada.</summary>
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Menu>> GetPendientesByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default);
}
