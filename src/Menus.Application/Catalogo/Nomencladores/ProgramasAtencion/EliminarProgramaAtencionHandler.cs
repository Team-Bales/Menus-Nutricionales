using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.ProgramasAtencion;

/// <summary>Elimina el programa de atención si no está en uso.</summary>
/// <remarks>
/// El uso se comprueba antes para dar un mensaje claro; la FK en <c>RESTRICT</c> de <c>Dieta</c> sigue siendo la
/// última barrera (ADR-010, §3).
/// </remarks>
internal sealed class EliminarProgramaAtencionHandler(
    INomencladorRepository<ProgramaAtencion> programas,
    IUnitOfWork unidadDeTrabajo) : IHandler<EliminarProgramaAtencionCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el programa no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si alguna dieta pertenece a este programa (RR-12).</exception>
    public async Task<Unidad> HandleAsync(EliminarProgramaAtencionCommand solicitud, CancellationToken ct)
    {
        var programa = await programas.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("programa de atención", solicitud.Id);

        if (await programas.EstaEnUsoAsync(programa.Id, ct))
        {
            throw new ReglaDeNegocioException(
                $"El programa de atención «{programa.Nombre}» tiene dietas: no se puede eliminar.", "RR-12");
        }

        programas.Remove(programa);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
