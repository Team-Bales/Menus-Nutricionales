using Menus.Application.Comun;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.TiposPreparacion;

/// <summary>
/// Devuelve el catálogo tal como lo define el dominio: es fijo (ADR-008), así que no hace falta ir a la base.
/// </summary>
internal sealed class ListarTiposPreparacionHandler : IHandler<ListarTiposPreparacionQuery, IReadOnlyList<NomencladorDto>>
{
    public Task<IReadOnlyList<NomencladorDto>> HandleAsync(ListarTiposPreparacionQuery solicitud, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<NomencladorDto>>([.. TipoPreparacion.Todos.Select(NomencladorDto.Desde)]);
}
