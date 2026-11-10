using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Valoraciones.ListarValoracionesPorDieta;

/// <summary>Comprueba que la dieta exista y devuelve sus valoraciones.</summary>
internal sealed class ListarValoracionesPorDietaHandler(
    IDietaRepository dietas,
    IValoracionRepository valoraciones) : IHandler<ListarValoracionesPorDietaQuery, IReadOnlyList<ValoracionDto>>
{
    /// <exception cref="NoEncontradoException">Si la dieta no existe.</exception>
    public async Task<IReadOnlyList<ValoracionDto>> HandleAsync(ListarValoracionesPorDietaQuery solicitud, CancellationToken ct)
    {
        _ = await dietas.GetByIdAsync(solicitud.DietaId, ct)
            ?? throw new NoEncontradoException("dieta", solicitud.DietaId);

        var resultado = await valoraciones.GetByDietaAsync(solicitud.DietaId, ct);
        return [.. resultado.Select(ValoracionDto.Desde)];
    }
}
