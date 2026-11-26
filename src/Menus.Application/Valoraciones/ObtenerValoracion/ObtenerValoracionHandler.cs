using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Valoraciones.ObtenerValoracion;

/// <summary>Busca la valoración por su identificador.</summary>
/// <remarks>
/// La ven el personal (nutricionista o administrador) y el paciente valorado (#285). El identificador de la ruta es el
/// de la valoración, así que la API no puede saber de quién es: lo comprueba este caso de uso con la identidad del
/// token. Es la valoración que el paciente revisa cuando pide su revalorización (RS-09, #267).
/// </remarks>
internal sealed class ObtenerValoracionHandler(IValoracionRepository valoraciones, IIdentidadAutenticada identidad)
    : IHandler<ObtenerValoracionQuery, ValoracionDto>
{
    /// <exception cref="NoEncontradoException">Si la valoración no existe.</exception>
    /// <exception cref="AccesoDenegadoException">Si la pide un paciente que no es el valorado.</exception>
    public async Task<ValoracionDto> HandleAsync(ObtenerValoracionQuery solicitud, CancellationToken ct)
    {
        var valoracion = await valoraciones.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("valoración", solicitud.Id);

        if (!identidad.EsPersonalOElPaciente(valoracion.PacienteId))
        {
            throw new AccesoDenegadoException("Solo el paciente valorado y el personal ven esta valoración.");
        }

        return ValoracionDto.Desde(valoracion);
    }
}
