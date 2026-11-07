using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.Especialidades;

/// <summary>Devuelve todas las especialidades, ordenadas por nombre.</summary>
internal sealed class ListarEspecialidadesHandler(INomencladorRepository<Especialidad> especialidades)
    : IHandler<ListarEspecialidadesQuery, IReadOnlyList<NomencladorDto>>
{
    public async Task<IReadOnlyList<NomencladorDto>> HandleAsync(ListarEspecialidadesQuery solicitud, CancellationToken ct)
    {
        var todas = await especialidades.ListAsync(ct);
        return [.. todas.OrderBy(e => e.Nombre, StringComparer.Ordinal).Select(NomencladorDto.Desde)];
    }
}
