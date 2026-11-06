using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Dietas.ObtenerDieta;

/// <summary>Busca la dieta por su identificador, con sus grupos y restricciones cargados.</summary>
internal sealed class ObtenerDietaHandler(IDietaRepository dietas) : IHandler<ObtenerDietaQuery, DietaDto>
{
    /// <exception cref="NoEncontradoException">Si la dieta no existe.</exception>
    public async Task<DietaDto> HandleAsync(ObtenerDietaQuery solicitud, CancellationToken ct)
    {
        var dieta = await dietas.GetConGruposYRestriccionesAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("dieta", solicitud.Id);

        return DietaDto.Desde(dieta);
    }
}
