using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.ProgramasAtencion;

/// <summary>Devuelve todos los programas de atención, ordenados por nombre.</summary>
internal sealed class ListarProgramasAtencionHandler(INomencladorRepository<ProgramaAtencion> programas)
    : IHandler<ListarProgramasAtencionQuery, IReadOnlyList<NomencladorDto>>
{
    public async Task<IReadOnlyList<NomencladorDto>> HandleAsync(ListarProgramasAtencionQuery solicitud, CancellationToken ct)
    {
        var todos = await programas.ListAsync(ct);
        return [.. todos.OrderBy(p => p.Nombre, StringComparer.Ordinal).Select(NomencladorDto.Desde)];
    }
}
