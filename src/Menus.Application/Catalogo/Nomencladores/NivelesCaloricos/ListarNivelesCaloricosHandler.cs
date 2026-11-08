using Menus.Application.Comun;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.NivelesCaloricos;

/// <summary>
/// Devuelve el catálogo tal como lo define el dominio: es fijo (ADR-008), así que no hace falta ir a la base.
/// </summary>
internal sealed class ListarNivelesCaloricosHandler : IHandler<ListarNivelesCaloricosQuery, IReadOnlyList<NomencladorDto>>
{
    public Task<IReadOnlyList<NomencladorDto>> HandleAsync(ListarNivelesCaloricosQuery solicitud, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<NomencladorDto>>([.. NivelCalorico.Todos.Select(NomencladorDto.Desde)]);
}
