using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Pacientes.ListarPacientes;

/// <summary>Devuelve la página pedida en el sobre <c>{ data, total, page, size }</c> (ADR-010, §2).</summary>
internal sealed class ListarPacientesHandler(IPacienteRepository pacientes)
    : IHandler<ListarPacientesQuery, ResultadoPaginado<PacienteDto>>
{
    public async Task<ResultadoPaginado<PacienteDto>> HandleAsync(ListarPacientesQuery solicitud, CancellationToken ct)
    {
        var (pagina, total) = await pacientes.BuscarAsync(
            solicitud.SalaId,
            solicitud.DietaId,
            solicitud.Page,
            solicitud.Size,
            ct);

        return new ResultadoPaginado<PacienteDto>(
            [.. pagina.Select(PacienteDto.Desde)],
            total,
            solicitud.Page,
            solicitud.Size);
    }
}
