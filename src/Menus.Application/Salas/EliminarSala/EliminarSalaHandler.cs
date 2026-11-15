using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.Salas.EliminarSala;

/// <summary>Elimina la sala si no tiene pacientes asignados.</summary>
/// <remarks>Se comprueba antes para dar un mensaje claro; la clave foránea en <c>RESTRICT</c> sigue siendo la
/// última barrera (ADR-010, §3).</remarks>
internal sealed class EliminarSalaHandler(
    ISalaRepository salas,
    IPacienteRepository pacientes,
    IUnitOfWork unidadDeTrabajo) : IHandler<EliminarSalaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si la sala no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si tiene pacientes asignados.</exception>
    public async Task<Unidad> HandleAsync(EliminarSalaCommand solicitud, CancellationToken ct)
    {
        var sala = await salas.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("sala", solicitud.Id);

        if ((await pacientes.GetBySalaAsync(sala.Id, ct)).Count > 0)
        {
            throw new ReglaDeNegocioException(
                $"La sala «{sala.Nombre}» tiene pacientes asignados: hay que trasladarlos antes de eliminarla.");
        }

        salas.Remove(sala);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
