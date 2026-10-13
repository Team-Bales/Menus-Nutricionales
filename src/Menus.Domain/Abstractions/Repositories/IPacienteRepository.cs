using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="Paciente"/>. Además de las operaciones base, expone los filtros por sala
/// (relación <c>Pertenece</c>) y por dieta (relación <c>Inscribe</c>) que necesita la gestión de
/// pacientes (issue #73).
/// </summary>
/// <remarks>
/// <see cref="IRepository{TEntity, TId}.GetByIdAsync"/> devuelve el paciente con sus dietas inscritas
/// (<see cref="Paciente.Dietas"/>) cargadas, porque la regla de no repetir una inscripción (R6-04)
/// se comprueba sobre esa colección.
/// </remarks>
public interface IPacienteRepository : IRepository<Paciente, Guid>
{
    /// <summary>Devuelve los pacientes de una sala (relación <c>Pertenece</c>).</summary>
    /// <param name="salaId">Identificador de la sala.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Paciente>> GetBySalaAsync(Guid salaId, CancellationToken cancellationToken = default);

    /// <summary>Devuelve los pacientes inscritos en una dieta (relación <c>Inscribe</c>).</summary>
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Paciente>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default);
}
