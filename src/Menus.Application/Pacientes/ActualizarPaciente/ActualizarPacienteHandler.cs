using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.Pacientes.ActualizarPaciente;

/// <summary>Actualiza los datos del paciente y, si la sala cambia, lo traslada.</summary>
internal sealed class ActualizarPacienteHandler(
    IPacienteRepository pacientes,
    IRepository<Sala, Guid> salas,
    IUnitOfWork unidadDeTrabajo) : IHandler<ActualizarPacienteCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el paciente no existe, o si la sala nueva no existe.</exception>
    public async Task<Unidad> HandleAsync(ActualizarPacienteCommand solicitud, CancellationToken ct)
    {
        var paciente = await pacientes.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("paciente", solicitud.Id);

        // Solo se busca la sala si cambia: la actual existe, porque una sala con pacientes no se borra (RR-10).
        if (solicitud.SalaId != paciente.SalaId)
        {
            _ = await salas.GetByIdAsync(solicitud.SalaId, ct)
                ?? throw new NoEncontradoException("sala", solicitud.SalaId);

            paciente.TrasladarASala(solicitud.SalaId);
        }

        paciente.ActualizarDatos(solicitud.Nombre.Trim(), solicitud.Edad);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
