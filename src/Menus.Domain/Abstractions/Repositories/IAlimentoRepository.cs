using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="Alimento"/>. Además de las operaciones base, expone las consultas
/// que necesita el catálogo de alimentos (issues #60 y #61).
/// </summary>
/// <remarks>
/// Que el nombre no esté repetido se comprueba con <see cref="IItemAlimentarioRepository.ExisteNombreAsync"/>,
/// porque abarca también a los platos (RC-01).
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

    /// <summary>
    /// Devuelve una página de alimentos, ordenados por nombre, con los filtros que se indiquen, y el total sin paginar.
    /// </summary>
    /// <param name="grupoNutricionalId">Solo los de este grupo nutricional, si se indica.</param>
    /// <param name="tipoPreparacionId">Solo los de este tipo de preparación, si se indica.</param>
    /// <param name="nivelCaloricoId">Solo los de este nivel calórico, si se indica.</param>
    /// <param name="pagina">Número de página, desde 1.</param>
    /// <param name="tamano">Cantidad de alimentos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Los alimentos de la página y el total de los que cumplen los filtros.</returns>
    Task<(IReadOnlyList<Alimento> Alimentos, int Total)> BuscarAsync(
        Guid? grupoNutricionalId,
        Guid? tipoPreparacionId,
        Guid? nivelCaloricoId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si el alimento es ingrediente de algún plato (relación <c>Compone</c>). Un alimento así no se
    /// elimina (RR-04).
    /// </summary>
    /// <param name="alimentoId">Identificador del alimento.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<bool> FormaParteDeAlgunPlatoAsync(Guid alimentoId, CancellationToken cancellationToken = default);
}
