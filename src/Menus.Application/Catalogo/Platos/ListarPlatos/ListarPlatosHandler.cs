using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Platos.ListarPlatos;

/// <summary>
/// Devuelve la página pedida en el sobre <c>{ data, total, page, size }</c> (ADR-010, §2), con la cantidad de
/// ingredientes de cada plato contada en una sola consulta.
/// </summary>
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

        var cantidades = await platos.ContarIngredientesAsync([.. pagina.Select(p => p.Id)], ct);

        return new ResultadoPaginado<PlatoDto>(
            [.. pagina.Select(p => PlatoDto.Desde(p, cantidades.GetValueOrDefault(p.Id)))],
            total,
            solicitud.Page,
            solicitud.Size);
    }
}
