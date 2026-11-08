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

    /// <summary>Busca dietas por página, ordenadas por nombre, con filtro opcional por programa de atención (#63).</summary>
    /// <param name="programaAtencionId">Si no es nulo, solo las de ese programa.</param>
    /// <param name="pagina">Número de página, desde 1.</param>
    /// <param name="tamano">Tamaño de página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La página pedida y el total de dietas que cumplen el filtro, sin paginar.</returns>
    Task<(IReadOnlyList<Dieta> Dietas, int Total)> BuscarAsync(
        Guid? programaAtencionId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve, sin rastrear y sin sus grupos ni restricciones, las dietas de esos identificadores que existen.
    /// </summary>
    /// <remarks>Lo usa el detalle del paciente (#73), para mostrar el nombre de sus dietas inscritas.</remarks>
    /// <param name="ids">Identificadores buscados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Dieta>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
}
