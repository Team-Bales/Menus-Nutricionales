using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;

namespace Menus.Application.GestionDeMenus.ObtenerMenu;

/// <summary>
/// Busca el menú completo y les pone nombre a sus nutricionistas, a sus tipos de preparación, a sus grupos
/// nutricionales y a sus ítems (<see cref="DetallesDeMenus"/>).
/// </summary>
internal sealed class ObtenerMenuHandler(
    IMenuRepository menus,
    INutricionistaRepository nutricionistas,
    INomencladorRepository<GrupoNutricional> grupos,
    IItemAlimentarioRepository items) : IHandler<ObtenerMenuQuery, MenuDetalleDto>
{
    /// <exception cref="NoEncontradoException">Si el menú no existe.</exception>
    public async Task<MenuDetalleDto> HandleAsync(ObtenerMenuQuery solicitud, CancellationToken ct)
    {
        var menu = await menus.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("menú", solicitud.Id);

        return (await new DetallesDeMenus(nutricionistas, grupos, items).ArmarAsync([menu], ct))[0];
    }
}
