using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Valoraciones.ListarValoracionesPorPaciente;

/// <summary>
/// Comprueba que el paciente exista y devuelve sus valoraciones: las más recientes, servidas desde Redis cuando
/// está disponible (informe, «R2»; patrón <i>cache-aside</i>, ADR-005).
/// </summary>
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
