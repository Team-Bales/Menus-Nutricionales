using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="Plato"/>. Además de las operaciones base, expone las consultas
/// que necesita el catálogo de platos (issues #60 y #62).
/// </summary>
/// <remarks>
/// Que el nombre no esté repetido se comprueba con <see cref="IItemAlimentarioRepository.ExisteNombreAsync"/>,
/// porque abarca también a los alimentos (RC-01, extendida a los platos por decisión del equipo: submodelo #5;
/// con la generalización <see cref="ItemAlimentario"/>, los dos comparten un mismo espacio de nombres).
/// </remarks>
public interface IPlatoRepository : IRepository<Plato, Guid>
{
    /// <summary>Devuelve un plato con sus ingredientes (<see cref="Plato.Ingredientes"/>) cargados.</summary>
    /// <param name="id">Identificador del plato.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El plato, o <see langword="null"/> si no existe.</returns>
    Task<Plato?> GetConIngredientesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve una página de platos, sin sus ingredientes, ordenados por nombre, con los filtros que se indiquen, y el
    /// total sin paginar.
    /// </summary>
    /// <param name="grupoNutricionalId">Solo los de este grupo nutricional, si se indica.</param>
    /// <param name="tipoPreparacionId">Solo los de este tipo de preparación, si se indica.</param>
    /// <param name="nivelCaloricoId">Solo los de este nivel calórico, si se indica.</param>
    /// <param name="pagina">Número de página, desde 1.</param>
    /// <param name="tamano">Cantidad de platos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Los platos de la página y el total de los que cumplen los filtros.</returns>
    Task<(IReadOnlyList<Plato> Platos, int Total)> BuscarAsync(
        Guid? grupoNutricionalId,
        Guid? tipoPreparacionId,
        Guid? nivelCaloricoId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default);
}
