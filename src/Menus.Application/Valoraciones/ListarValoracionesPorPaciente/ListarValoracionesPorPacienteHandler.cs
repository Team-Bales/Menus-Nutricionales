using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Valoraciones.ListarValoracionesPorPaciente;

/// <summary>Comprueba que el paciente exista y devuelve sus valoraciones.</summary>
internal sealed class ListarValoracionesPorPacienteHandler(
    IPacienteRepository pacientes,
    IValoracionRepository valoraciones) : IHandler<ListarValoracionesPorPacienteQuery, IReadOnlyList<ValoracionDto>>
{
    /// <exception cref="NoEncontradoException">Si el paciente no existe.</exception>
    public async Task<IReadOnlyList<ValoracionDto>> HandleAsync(ListarValoracionesPorPacienteQuery solicitud, CancellationToken ct)
    {
        _ = await pacientes.GetByIdAsync(solicitud.PacienteId, ct)
            ?? throw new NoEncontradoException("paciente", solicitud.PacienteId);

        var resultado = await valoraciones.GetByPacienteAsync(solicitud.PacienteId, ct);
        return [.. resultado.Select(ValoracionDto.Desde)];
    }
}
