using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="Paciente"/>. Además de las operaciones base, expone los filtros por sala
/// (relación <c>Pertenece</c>) y por dieta (relación <c>Inscribe</c>) que necesita la gestión de
/// pacientes (issue #73).
/// </summary>
/// <remarks>
/// <see cref="IRepository{TEntity, TId}.GetByIdAsync"/> devuelve el paciente con sus dietas inscritas
/// (<see cref="Paciente.Dietas"/>) cargadas, porque la regla de no repetir una inscripción (RC-07)
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

    /// <summary>
    /// Devuelve una página de pacientes, sin sus inscripciones, ordenados por nombre, con los filtros que se indiquen,
    /// y el total sin paginar.
    /// </summary>
    /// <param name="salaId">Solo los de esta sala, si se indica.</param>
    /// <param name="dietaId">Solo los inscritos en esta dieta, si se indica.</param>
    /// <param name="pagina">Número de página, desde 1.</param>
    /// <param name="tamano">Cantidad de pacientes por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Los pacientes de la página y el total de los que cumplen los filtros.</returns>
    Task<(IReadOnlyList<Paciente> Pacientes, int Total)> BuscarAsync(
        Guid? salaId,
        Guid? dietaId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default);
}
