using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Pacientes.InscribirDieta;

/// <summary>Inscribe al paciente en la dieta, o no hace nada si ya está inscrito (idempotente).</summary>
internal sealed class InscribirDietaHandler(
    IPacienteRepository pacientes,
    IDietaRepository dietas,
    IUnitOfWork unidadDeTrabajo) : IHandler<InscribirDietaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el paciente no existe, o si la dieta no existe.</exception>
    public async Task<Unidad> HandleAsync(InscribirDietaCommand solicitud, CancellationToken ct)
    {
        var paciente = await pacientes.GetByIdAsync(solicitud.PacienteId, ct)
            ?? throw new NoEncontradoException("paciente", solicitud.PacienteId);

        // Si ya está inscrito, la dieta existe (la inscripción la referencia con una FK) y no queda nada que hacer.
        if (paciente.Dietas.Any(d => d.DietaId == solicitud.DietaId))
        {
            return Unidad.Valor;
        }

        _ = await dietas.GetByIdAsync(solicitud.DietaId, ct)
            ?? throw new NoEncontradoException("dieta", solicitud.DietaId);

        paciente.InscribirDieta(solicitud.DietaId);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
