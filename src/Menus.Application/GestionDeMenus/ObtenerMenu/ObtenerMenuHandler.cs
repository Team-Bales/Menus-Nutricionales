using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;

namespace Menus.Application.GestionDeMenus.ObtenerMenu;

/// <summary>
/// Busca el menú completo y les pone nombre a sus nutricionistas, a sus tipos de preparación, a sus grupos
/// nutricionales y a sus ítems.
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

        var resumen = MenuDto.Desde(
            menu,
            await NombresDeNutricionistas.BuscarAsync(nutricionistas, menu.DietaId, NombresDeNutricionistas.De(menu), ct));

        // Los tipos de preparación son un catálogo cerrado (ADR-008): su orden es el del catálogo.
        IReadOnlyList<ProporcionDelMenuDto> distribucion =
        [
            .. TipoPreparacion.Todos
                .Join(menu.Distribuciones, t => t.Id, d => d.TipoPreparacionId, (t, d) => new ProporcionDelMenuDto(t.Id, t.Nombre, d.Proporcion)),
        ];

        // Los grupos nutricionales son pocos: se traen todos en una consulta y se cruzan aquí.
        var cubiertos = menu.GruposNutricionales.Select(g => g.GrupoNutricionalId).ToHashSet();
        var gruposDelMenu = (await grupos.ListAsync(ct))
            .Where(g => cubiertos.Contains(g.Id))
            .Select(g => new ElementoDelMenuDto(g.Id, g.Nombre));

        var itemsDelMenu = (await items.GetByIdsAsync([.. menu.Items.Select(i => i.ItemId)], ct))
            .Select(i => new ElementoDelMenuDto(i.Id, i.Nombre));

        return new MenuDetalleDto(
            resumen.Id,
            resumen.DietaId,
            resumen.FechaCreacion,
            resumen.Creador,
            resumen.EsAutomatico,
            resumen.CantidadTotalAlimentos,
            resumen.Estado,
            resumen.Dictamen,
            resumen.ReemplazaA,
            distribucion,
            Ordenados(gruposDelMenu),
            Ordenados(itemsDelMenu));
    }

    private static IReadOnlyList<ElementoDelMenuDto> Ordenados(IEnumerable<ElementoDelMenuDto> elementos)
        => [.. elementos.OrderBy(e => e.Nombre, StringComparer.CurrentCulture)];
}
