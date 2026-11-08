using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Pacientes.DesinscribirDieta;

/// <summary>Retira la inscripción del paciente en la dieta.</summary>
internal sealed class DesinscribirDietaHandler(IPacienteRepository pacientes, IUnitOfWork unidadDeTrabajo)
    : IHandler<DesinscribirDietaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el paciente no existe, o si no está inscrito en la dieta.</exception>
    public async Task<Unidad> HandleAsync(DesinscribirDietaCommand solicitud, CancellationToken ct)
    {
        var paciente = await pacientes.GetByIdAsync(solicitud.PacienteId, ct)
            ?? throw new NoEncontradoException("paciente", solicitud.PacienteId);

        // La ruta nombra la inscripción (DELETE …/dietas/{dietaId}): si no está, el recurso no existe.
        if (!paciente.Dietas.Any(d => d.DietaId == solicitud.DietaId))
        {
            throw new NoEncontradoException("paciente inscrito en la dieta", solicitud.DietaId);
        }

        paciente.DesinscribirDieta(solicitud.DietaId);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
