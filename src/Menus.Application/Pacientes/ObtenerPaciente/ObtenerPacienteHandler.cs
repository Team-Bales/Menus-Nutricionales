using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.Pacientes.ObtenerPaciente;

/// <summary>Busca el paciente con sus inscripciones, y les pone nombre a su sala y a sus dietas.</summary>
internal sealed class ObtenerPacienteHandler(
    IPacienteRepository pacientes,
    IRepository<Sala, Guid> salas,
    IDietaRepository dietas) : IHandler<ObtenerPacienteQuery, PacienteDetalleDto>
{
    /// <exception cref="NoEncontradoException">Si el paciente no existe.</exception>
    public async Task<PacienteDetalleDto> HandleAsync(ObtenerPacienteQuery solicitud, CancellationToken ct)
    {
        var paciente = await pacientes.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("paciente", solicitud.Id);

        // La sala y las dietas existen: las referencian claves foráneas con ON DELETE RESTRICT (RR-10, RR-12).
        var sala = await salas.GetByIdAsync(paciente.SalaId, ct);
        var inscritas = await dietas.GetByIdsAsync([.. paciente.Dietas.Select(d => d.DietaId)], ct);

        IReadOnlyList<DietaInscritaDto> dietasInscritas =
        [
            .. inscritas
                .Select(d => new DietaInscritaDto(d.Id, d.Nombre))
                .OrderBy(d => d.Nombre, StringComparer.CurrentCulture),
        ];

        return new PacienteDetalleDto(
            paciente.Id,
            paciente.Nombre,
            paciente.Edad,
            new SalaDelPacienteDto(paciente.SalaId, sala!.Nombre),
            dietasInscritas);
    }
}
