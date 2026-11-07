using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Platos.ListarPlatos;

/// <summary>Devuelve la página pedida en el sobre <c>{ data, total, page, size }</c> (ADR-010, §2).</summary>
internal sealed class ListarPlatosHandler(IPlatoRepository platos)
    : IHandler<ListarPlatosQuery, ResultadoPaginado<PlatoDto>>
{
    public async Task<ResultadoPaginado<PlatoDto>> HandleAsync(ListarPlatosQuery solicitud, CancellationToken ct)
    {
        var (pagina, total) = await platos.BuscarAsync(
            solicitud.GrupoNutricionalId,
            solicitud.TipoPreparacionId,
            solicitud.NivelCaloricoId,
            solicitud.Page,
            solicitud.Size,
            ct);

        return new ResultadoPaginado<PlatoDto>(
            [.. pagina.Select(PlatoDto.Desde)],
            total,
            solicitud.Page,
            solicitud.Size);
    }
}
