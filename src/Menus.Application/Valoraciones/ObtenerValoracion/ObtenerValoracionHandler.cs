using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Valoraciones.ObtenerValoracion;

/// <summary>Busca la valoración por su identificador.</summary>
internal sealed class ObtenerValoracionHandler(IValoracionRepository valoraciones)
    : IHandler<ObtenerValoracionQuery, ValoracionDto>
{
    /// <exception cref="NoEncontradoException">Si la valoración no existe.</exception>
    public async Task<ValoracionDto> HandleAsync(ObtenerValoracionQuery solicitud, CancellationToken ct)
    {
        var valoracion = await valoraciones.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("valoración", solicitud.Id);

        return ValoracionDto.Desde(valoracion);
    }
}
