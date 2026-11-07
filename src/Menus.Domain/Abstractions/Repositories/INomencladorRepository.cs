namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de un nomenclador institucional abierto (<see cref="Entity{TId}"/> con <c>Guid</c> y un nombre
/// único): <see cref="Nomenclators.GrupoNutricional"/>, <see cref="Nomenclators.RestriccionAlimentaria"/>,
/// <see cref="Nomenclators.ProgramaAtencion"/> y <see cref="Nomenclators.Especialidad"/> (#64).
/// </summary>
/// <typeparam name="TNomenclador">El nomenclador.</typeparam>
public interface INomencladorRepository<TNomenclador> : IRepository<TNomenclador, Guid>
    where TNomenclador : class
{
    /// <summary>Indica si ya existe un nomenclador con ese nombre (índice único de la base).</summary>
    /// <param name="nombre">Nombre que se quiere registrar, ya recortado.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si algo fuera del propio nomenclador lo referencia, de modo que no se puede eliminar
    /// (RR-12 y las demás claves foráneas en <c>RESTRICT</c> que lo protegen).
    /// </summary>
    /// <param name="id">Identificador del nomenclador.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<bool> EstaEnUsoAsync(Guid id, CancellationToken cancellationToken = default);
}
