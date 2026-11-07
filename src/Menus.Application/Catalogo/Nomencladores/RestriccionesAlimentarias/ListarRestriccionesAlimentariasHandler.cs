using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.RestriccionesAlimentarias;

/// <summary>Devuelve todas las restricciones alimentarias, ordenadas por nombre.</summary>
internal sealed class ListarRestriccionesAlimentariasHandler(INomencladorRepository<RestriccionAlimentaria> restricciones)
    : IHandler<ListarRestriccionesAlimentariasQuery, IReadOnlyList<NomencladorDto>>
{
    public async Task<IReadOnlyList<NomencladorDto>> HandleAsync(ListarRestriccionesAlimentariasQuery solicitud, CancellationToken ct)
    {
        var todas = await restricciones.ListAsync(ct);
        return [.. todas.OrderBy(r => r.Nombre, StringComparer.Ordinal).Select(NomencladorDto.Desde)];
    }
}
