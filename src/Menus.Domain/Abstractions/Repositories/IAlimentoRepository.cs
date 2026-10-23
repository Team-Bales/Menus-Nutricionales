using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="Alimento"/>. Además de las operaciones base, expone las consultas
/// que necesita el catálogo de alimentos (issues #60 y #61).
/// </summary>
/// <remarks>
/// Que el nombre no esté repetido se comprueba con <see cref="IItemAlimentarioRepository.ExisteNombreAsync"/>,
/// porque abarca también a los platos (R5-16).
/// </remarks>
public interface IAlimentoRepository : IRepository<Alimento, Guid>
{
    /// <summary>Devuelve los alimentos de un grupo nutricional.</summary>
    /// <param name="grupoNutricionalId">Identificador del grupo nutricional.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Alimento>> GetByGrupoNutricionalAsync(Guid grupoNutricionalId, CancellationToken cancellationToken = default);

    /// <summary>Devuelve los alimentos de un nivel calórico.</summary>
    /// <param name="nivelCalorico">Nivel calórico buscado.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<Alimento>> GetByNivelCaloricoAsync(NivelCalorico nivelCalorico, CancellationToken cancellationToken = default);
}
