using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Alimentos.ListarAlimentos;

/// <summary>Devuelve la página pedida en el sobre <c>{ data, total, page, size }</c> (ADR-010, §2).</summary>
internal sealed class ListarAlimentosHandler(IAlimentoRepository alimentos)
    : IHandler<ListarAlimentosQuery, ResultadoPaginado<AlimentoDto>>
{
    public async Task<ResultadoPaginado<AlimentoDto>> HandleAsync(ListarAlimentosQuery solicitud, CancellationToken ct)
    {
        var (pagina, total) = await alimentos.BuscarAsync(
            solicitud.GrupoNutricionalId,
            solicitud.TipoPreparacionId,
            solicitud.NivelCaloricoId,
            solicitud.Page,
            solicitud.Size,
            ct);

        return new ResultadoPaginado<AlimentoDto>(
            [.. pagina.Select(AlimentoDto.Desde)],
            total,
            solicitud.Page,
            solicitud.Size);
    }
}
