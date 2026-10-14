using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="ValoracionNutricional"/>. Además de las operaciones base, expone las
/// consultas que necesita la gestión de valoraciones por nutricionistas y pacientes (issues #86 y #88).
/// </summary>
/// <remarks>
/// La valoración no guarda la dieta: evalúa una asignación (relación <c>Evalúa</c>, R7-12), y la dieta
/// es la del menú de esa asignación. Los filtros por dieta se resuelven a través de ese menú.
/// </remarks>
public interface IValoracionRepository : IRepository<ValoracionNutricional, Guid>
{
    /// <summary>Devuelve las valoraciones de un paciente, de todas sus dietas.</summary>
    /// <param name="pacienteId">Identificador del paciente evaluado.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<ValoracionNutricional>> GetByPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default);

    /// <summary>Devuelve las valoraciones de los menús de una dieta, de todos sus pacientes.</summary>
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<ValoracionNutricional>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default);

    /// <summary>Devuelve las valoraciones de un paciente sobre los menús de una dieta.</summary>
    /// <param name="pacienteId">Identificador del paciente evaluado.</param>
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<ValoracionNutricional>> GetByPacienteYDietaAsync(Guid pacienteId, Guid dietaId, CancellationToken cancellationToken = default);
}
