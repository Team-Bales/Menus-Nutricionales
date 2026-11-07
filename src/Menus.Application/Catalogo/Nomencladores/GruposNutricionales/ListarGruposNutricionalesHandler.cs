using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.GruposNutricionales;

/// <summary>Devuelve todos los grupos nutricionales, ordenados por nombre.</summary>
internal sealed class ListarGruposNutricionalesHandler(INomencladorRepository<GrupoNutricional> grupos)
    : IHandler<ListarGruposNutricionalesQuery, IReadOnlyList<NomencladorDto>>
{
    public async Task<IReadOnlyList<NomencladorDto>> HandleAsync(ListarGruposNutricionalesQuery solicitud, CancellationToken ct)
    {
        var todos = await grupos.ListAsync(ct);
        return [.. todos.OrderBy(g => g.Nombre, StringComparer.Ordinal).Select(NomencladorDto.Desde)];
    }
}
