using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Salas.ListarSalas;

/// <summary>Devuelve todas las salas, ordenadas por nombre.</summary>
internal sealed class ListarSalasHandler(ISalaRepository salas) : IHandler<ListarSalasQuery, IReadOnlyList<SalaDto>>
{
    public async Task<IReadOnlyList<SalaDto>> HandleAsync(ListarSalasQuery solicitud, CancellationToken ct)
    {
        var todas = await salas.ListAsync(ct);
        return [.. todas.OrderBy(s => s.Nombre, StringComparer.Ordinal).Select(SalaDto.Desde)];
    }
}
