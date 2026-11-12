using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="ItemAlimentario"/>: las operaciones que abarcan a alimentos y platos a la vez.
/// Las consultas propias de cada clase están en <see cref="IAlimentoRepository"/> e <see cref="IPlatoRepository"/>.
/// </summary>
public interface IItemAlimentarioRepository : IRepository<ItemAlimentario, Guid>
{
    /// <summary>
    /// Indica si ya existe un alimento o un plato con ese nombre, sin distinguir mayúsculas ni espacios sobrantes
    /// (RC-01 / RS-01: no existen alimentos duplicados).
    /// </summary>
    /// <remarks>
    /// La comprobación abarca a las dos clases porque alimentos y platos son ítems de una misma generalización
    /// (<see cref="ItemAlimentario"/>) y comparten el espacio de nombres: un alimento y un plato no pueden
    /// llamarse igual. Por eso está aquí y no en los repositorios de cada clase.
    /// </remarks>
    /// <param name="nombre">Nombre que se quiere registrar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si el ítem figura en algún menú (relación <c>Incluye</c>). Un ítem así no se elimina (RR-22).
    /// </summary>
    /// <remarks>
    /// Abarca a todos los menús, no solo a los vigentes: el historial de un menú ya servido o dictaminado depende de
    /// sus ítems, y la base lo protege con <c>ON DELETE RESTRICT</c> (informe, §4.4).
    /// </remarks>
    /// <param name="itemId">Identificador del alimento o el plato.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<bool> FiguraEnAlgunMenuAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve, sin rastrear, los alimentos y platos de esos identificadores que existen.
    /// </summary>
    /// <remarks>Lo usa el detalle del menú (#80), para mostrar el nombre de sus ítems.</remarks>
    /// <param name="ids">Identificadores buscados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<ItemAlimentario>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cuenta los ítems del banco por nivel calórico, excluyendo los que pertenecen a alguno de los grupos
    /// prohibidos (informe, «R1 — Disponibilidad calórica», #101).
    /// </summary>
    /// <param name="gruposProhibidos">Grupos nutricionales que no cuentan (<see cref="IDietaRepository.GetGruposProhibidosAsync"/>).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El conteo por nivel calórico; un nivel sin ítems compatibles no aparece.</returns>
    Task<IReadOnlyDictionary<Guid, int>> ContarPorNivelCaloricoAsync(
        IReadOnlyCollection<Guid> gruposProhibidos,
        CancellationToken cancellationToken = default);
}
