using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Nutricionistas.ListarNutricionistas;

/// <summary>Devuelve la página pedida en el sobre <c>{ data, total, page, size }</c> (ADR-010, §2).</summary>
internal sealed class ListarNutricionistasHandler(INutricionistaRepository nutricionistas)
    : IHandler<ListarNutricionistasQuery, ResultadoPaginado<NutricionistaDto>>
{
    public async Task<ResultadoPaginado<NutricionistaDto>> HandleAsync(ListarNutricionistasQuery solicitud, CancellationToken ct)
    {
        var (pagina, total) = await nutricionistas.BuscarAsync(solicitud.EspecialidadId, solicitud.Page, solicitud.Size, ct);

        return new ResultadoPaginado<NutricionistaDto>(
            [.. pagina.Select(NutricionistaDto.Desde)],
            total,
            solicitud.Page,
            solicitud.Size);
    }
}
