using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;

/// <summary>
/// Calcula la disponibilidad calórica de una dieta (informe, «R1»), servida desde Redis cuando está disponible
/// (patrón <i>cache-aside</i>, ADR-005).
/// </summary>
internal sealed class DisponibilidadCaloricaHandler(
    IDietaRepository dietas,
    IItemAlimentarioRepository items,
    IMenuRepository menus,
    ICache cache) : IHandler<DisponibilidadCaloricaQuery, DisponibilidadCaloricaDto>
{
    /// <exception cref="NoEncontradoException">Si la dieta no existe.</exception>
    public async Task<DisponibilidadCaloricaDto> HandleAsync(DisponibilidadCaloricaQuery solicitud, CancellationToken ct)
    {
        var clave = CacheDeDisponibilidadCalorica.Clave(solicitud.DietaId);

        var cacheado = await cache.ObtenerAsync<DisponibilidadCaloricaDto>(clave, ct);
        if (cacheado is not null)
        {
            return cacheado;
        }

        _ = await dietas.GetByIdAsync(solicitud.DietaId, ct)
            ?? throw new NoEncontradoException("dieta", solicitud.DietaId);

        var gruposProhibidos = await dietas.GetGruposProhibidosAsync(solicitud.DietaId, ct);
        var delBanco = await items.ContarPorNivelCaloricoAsync(gruposProhibidos, ct);
        var deMenusFinales = await menus.ContarItemsFinalesPorNivelCaloricoAsync(solicitud.DietaId, ct);

        var porNivel = NivelCalorico.Todos
            .Select(n => new DisponibilidadPorNivelDto(
                n.Id,
                n.Nombre,
                delBanco.GetValueOrDefault(n.Id),
                deMenusFinales.GetValueOrDefault(n.Id)))
            .ToList();

        var resultado = new DisponibilidadCaloricaDto(solicitud.DietaId, porNivel);

        await cache.GuardarAsync(clave, resultado, CacheDeDisponibilidadCalorica.TiempoDeVida, ct);

        return resultado;
    }
}
