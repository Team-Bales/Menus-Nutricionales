using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="Revalorizacion"/>. Además de las operaciones base, expone las consultas
/// del flujo de revalorización: las solicitudes de un paciente y las que un nutricionista tiene por
/// ejecutar (issues #87 y #88).
/// </summary>
/// <remarks>
/// La revalorización tiene identidad y ciclo de vida propios: el paciente la solicita y el
/// nutricionista la completa después, al registrar la valoración generada. Por eso tiene repositorio,
/// aunque el ADR-002 no la incluya entre las raíces de agregado.
/// </remarks>
public interface IRevalorizacionRepository : IRepository<Revalorizacion, Guid>
{
    /// <summary>
    /// Devuelve las revalorizaciones que solicitó un paciente: las que revisan alguna de sus valoraciones. El
    /// solicitante no se guarda; se deduce de la valoración revisada (R7-29).
    /// </summary>
    /// <param name="pacienteId">Identificador del paciente solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Revalorizacion>> GetByPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve las revalorizaciones pendientes de un nutricionista: las que debe ejecutar y todavía
    /// no tienen valoración generada (<see cref="Revalorizacion.ValoracionGeneradaId"/> nulo, R7-24).
    /// </summary>
    /// <param name="nutricionistaId">Identificador del nutricionista que ejecuta la revalorización.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Revalorizacion>> GetPendientesByNutricionistaAsync(Guid nutricionistaId, CancellationToken cancellationToken = default);
}
