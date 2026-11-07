using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Dietas.ListarDietas;

/// <summary>Devuelve la página pedida en el sobre <c>{ data, total, page, size }</c> (ADR-010, §2).</summary>
internal sealed class ListarDietasHandler(IDietaRepository dietas)
    : IHandler<ListarDietasQuery, ResultadoPaginado<DietaResumenDto>>
{
    public async Task<ResultadoPaginado<DietaResumenDto>> HandleAsync(ListarDietasQuery solicitud, CancellationToken ct)
    {
        var (pagina, total) = await dietas.BuscarAsync(solicitud.ProgramaAtencionId, solicitud.Page, solicitud.Size, ct);

        return new ResultadoPaginado<DietaResumenDto>(
            [.. pagina.Select(DietaResumenDto.Desde)],
            total,
            solicitud.Page,
            solicitud.Size);
    }
}
