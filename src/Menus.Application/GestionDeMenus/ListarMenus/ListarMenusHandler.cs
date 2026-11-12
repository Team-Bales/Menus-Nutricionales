using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.GestionDeMenus.ListarMenus;

/// <summary>
/// Devuelve la página pedida en el sobre <c>{ data, total, page, size }</c> (ADR-010, §2), con el nombre del creador y
/// del revisor de cada menú.
/// </summary>
internal sealed class ListarMenusHandler(
    IMenuRepository menus,
    IDietaRepository dietas,
    INutricionistaRepository nutricionistas) : IHandler<ListarMenusQuery, ResultadoPaginado<MenuDto>>
{
    /// <exception cref="NoEncontradoException">Si la dieta no existe.</exception>
    public async Task<ResultadoPaginado<MenuDto>> HandleAsync(ListarMenusQuery solicitud, CancellationToken ct)
    {
        // Sin esto, una dieta que no existe y una sin menús darían la misma página vacía.
        _ = await dietas.GetByIdAsync(solicitud.DietaId, ct)
            ?? throw new NoEncontradoException("dieta", solicitud.DietaId);

        var (pagina, total) = await menus.BuscarAsync(
            solicitud.DietaId,
            solicitud.Estado,
            solicitud.Page,
            solicitud.Size,
            ct);

        var nombres = await NombresDeNutricionistas.BuscarAsync(
            nutricionistas,
            solicitud.DietaId,
            pagina.SelectMany(NombresDeNutricionistas.De),
            ct);

        return new ResultadoPaginado<MenuDto>(
            [.. pagina.Select(m => MenuDto.Desde(m, nombres))],
            total,
            solicitud.Page,
            solicitud.Size);
    }
}
