using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Alimentos.ObtenerAlimento;

/// <summary>Busca el alimento por su identificador.</summary>
internal sealed class ObtenerAlimentoHandler(IAlimentoRepository alimentos) : IHandler<ObtenerAlimentoQuery, AlimentoDto>
{
    /// <exception cref="NoEncontradoException">Si el alimento no existe.</exception>
    public async Task<AlimentoDto> HandleAsync(ObtenerAlimentoQuery solicitud, CancellationToken ct)
    {
        var alimento = await alimentos.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("alimento", solicitud.Id);

        return AlimentoDto.Desde(alimento);
    }
}
