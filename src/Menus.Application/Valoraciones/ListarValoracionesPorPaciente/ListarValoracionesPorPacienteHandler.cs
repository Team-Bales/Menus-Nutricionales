using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Valoraciones.ListarValoracionesPorPaciente;

/// <summary>
/// Comprueba que el paciente exista y devuelve sus valoraciones: las más recientes, servidas desde Redis cuando
/// está disponible (informe, «R2»; patrón <i>cache-aside</i>, ADR-005).
/// </summary>
/// <remarks>
/// El informe define R2 como «las N valoraciones con mayor FechaValoración», pero el proyecto no fijó un valor de
/// N (deuda del #73/#86, no de esta caché): este caso de uso cachea y devuelve el historial completo, ya ordenado
/// de la más a la menos reciente, a propósito. Si se define N más adelante, se recorta aquí sin tocar la clave ni
/// el resto del patrón <i>cache-aside</i>.
/// </remarks>
internal sealed class ListarValoracionesPorPacienteHandler(
    IPacienteRepository pacientes,
    IValoracionRepository valoraciones,
    ICache cache) : IHandler<ListarValoracionesPorPacienteQuery, IReadOnlyList<ValoracionDto>>
{
    /// <exception cref="NoEncontradoException">Si el paciente no existe.</exception>
    public async Task<IReadOnlyList<ValoracionDto>> HandleAsync(ListarValoracionesPorPacienteQuery solicitud, CancellationToken ct)
    {
        var clave = CacheDeValoracionesRecientes.Clave(solicitud.PacienteId);

        var cacheado = await cache.ObtenerAsync<List<ValoracionDto>>(clave, ct);
        if (cacheado is not null)
        {
            return cacheado;
        }

        _ = await pacientes.GetByIdAsync(solicitud.PacienteId, ct)
            ?? throw new NoEncontradoException("paciente", solicitud.PacienteId);

        var resultado = await valoraciones.GetByPacienteAsync(solicitud.PacienteId, ct);
        List<ValoracionDto> dtos = [.. resultado.Select(ValoracionDto.Desde)];

        await cache.GuardarAsync(clave, dtos, CacheDeValoracionesRecientes.TiempoDeVida, ct);

        return dtos;
    }
}
