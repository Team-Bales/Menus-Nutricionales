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
        var conteos = await dietas.ContarRelacionesAsync([.. pagina.Select(d => d.Id)], ct);

        return new ResultadoPaginado<DietaResumenDto>(
            [.. pagina.Select(d => DietaResumenDto.Desde(d, conteos[d.Id]))],
            total,
            solicitud.Page,
            solicitud.Size);
    }
}
