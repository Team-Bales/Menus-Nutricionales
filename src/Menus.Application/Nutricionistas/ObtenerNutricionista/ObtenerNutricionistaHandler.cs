using Menus.Application.Comun;
using Menus.Domain;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Nutricionistas.ObtenerNutricionista;

/// <summary>Busca el nutricionista completo, con sus dietas autorizadas.</summary>
internal sealed class ObtenerNutricionistaHandler(
    INutricionistaRepository nutricionistas,
    IIdentidadAutenticada identidad) : IHandler<ObtenerNutricionistaQuery, NutricionistaDetalleDto>
{
    /// <exception cref="NoEncontradoException">Si el nutricionista no existe.</exception>
    /// <exception cref="AccesoDenegadoException">
    /// Si el actor no es Admin y no es el propio nutricionista consultado.
    /// </exception>
    public async Task<NutricionistaDetalleDto> HandleAsync(ObtenerNutricionistaQuery solicitud, CancellationToken ct)
    {
        if (identidad.Rol != RolesUsuario.Admin && identidad.EntidadId != solicitud.Id)
        {
            throw new AccesoDenegadoException("Solo se puede consultar el propio perfil de nutricionista.");
        }

        var nutricionista = await nutricionistas.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("nutricionista", solicitud.Id);

        return NutricionistaDetalleDto.Desde(nutricionista);
    }
}
