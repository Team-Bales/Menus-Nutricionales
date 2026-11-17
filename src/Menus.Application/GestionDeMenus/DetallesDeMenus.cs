using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Application.GestionDeMenus;

/// <summary>
/// Arma el <see cref="MenuDetalleDto"/> de uno o varios menús, con todos los nombres buscados en lote: una consulta
/// de nutricionistas por dieta, una de grupos nutricionales y una de ítems, sin importar cuántos menús sean.
/// </summary>
/// <remarks>Lo usan el detalle de un menú (#80) y los menús asignados a un paciente (#84).</remarks>
/// <param name="nutricionistas">Repositorio de nutricionistas.</param>
/// <param name="grupos">Catálogo de grupos nutricionales.</param>
/// <param name="items">Repositorio de alimentos y platos.</param>
internal sealed class DetallesDeMenus(
    INutricionistaRepository nutricionistas,
    INomencladorRepository<GrupoNutricional> grupos,
    IItemAlimentarioRepository items)
{
    /// <summary>El detalle de cada menú, en el mismo orden en que llegan.</summary>
    /// <param name="menus">Menús con su parametrización, sus ítems y su dictamen.</param>
    /// <param name="ct">Token de cancelación.</param>
    public async Task<IReadOnlyList<MenuDetalleDto>> ArmarAsync(IReadOnlyList<Menu> menus, CancellationToken ct)
    {
        var nombres = new Dictionary<Guid, string>();
        foreach (var deUnaDieta in menus.GroupBy(m => m.DietaId))
        {
            var deEsaDieta = await NombresDeNutricionistas.BuscarAsync(
                nutricionistas, deUnaDieta.Key, deUnaDieta.SelectMany(NombresDeNutricionistas.De), ct);
            foreach (var (id, nombre) in deEsaDieta)
            {
                nombres[id] = nombre;
            }
        }

        // Los grupos nutricionales son pocos: se traen todos en una consulta y se cruzan aquí.
        var nombresDeGrupos = (await grupos.ListAsync(ct)).ToDictionary(g => g.Id, g => g.Nombre);
        var nombresDeItems = (await items.GetByIdsAsync([.. menus.SelectMany(m => m.Items).Select(i => i.ItemId).Distinct()], ct))
            .ToDictionary(i => i.Id, i => i.Nombre);

        return [.. menus.Select(m => Detalle(m, nombres, nombresDeGrupos, nombresDeItems))];
    }

    private static MenuDetalleDto Detalle(
        Menu menu,
        IReadOnlyDictionary<Guid, string> nombres,
        IReadOnlyDictionary<Guid, string> nombresDeGrupos,
        IReadOnlyDictionary<Guid, string> nombresDeItems)
    {
        var resumen = MenuDto.Desde(menu, nombres);

        // Los tipos de preparación son un catálogo cerrado (ADR-008): su orden es el del catálogo.
        IReadOnlyList<ProporcionDelMenuDto> distribucion =
        [
            .. TipoPreparacion.Todos
                .Join(menu.Distribuciones, t => t.Id, d => d.TipoPreparacionId, (t, d) => new ProporcionDelMenuDto(t.Id, t.Nombre, d.Proporcion)),
        ];

        // En la base, los grupos y los ítems existen (claves foráneas en RESTRICT); si faltara alguno, se omite en lugar
        // de fallar el detalle entero, como hacía el detalle del #80.
        var gruposDelMenu = ConNombre(menu.GruposNutricionales.Select(g => g.GrupoNutricionalId), nombresDeGrupos);
        var itemsDelMenu = ConNombre(menu.Items.Select(i => i.ItemId), nombresDeItems);

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

    private static IEnumerable<ElementoDelMenuDto> ConNombre(IEnumerable<Guid> ids, IReadOnlyDictionary<Guid, string> nombres)
        => ids.Where(nombres.ContainsKey).Select(id => new ElementoDelMenuDto(id, nombres[id]));

    private static IReadOnlyList<ElementoDelMenuDto> Ordenados(IEnumerable<ElementoDelMenuDto> elementos)
        => [.. elementos.OrderBy(e => e.Nombre, StringComparer.CurrentCulture)];
}
