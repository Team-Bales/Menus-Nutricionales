using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Salas.ObtenerSala;

/// <summary>Busca la sala por su identificador.</summary>
internal sealed class ObtenerSalaHandler(ISalaRepository salas) : IHandler<ObtenerSalaQuery, SalaDto>
{
    /// <exception cref="NoEncontradoException">Si la sala no existe.</exception>
    public async Task<SalaDto> HandleAsync(ObtenerSalaQuery solicitud, CancellationToken ct)
    {
        var sala = await salas.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("sala", solicitud.Id);

        return SalaDto.Desde(sala);
    }
}
