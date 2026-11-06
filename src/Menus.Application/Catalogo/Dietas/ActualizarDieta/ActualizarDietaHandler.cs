using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Dietas.ActualizarDieta;

/// <summary>Actualiza el nombre y el programa de atención de la dieta.</summary>
internal sealed class ActualizarDietaHandler(IDietaRepository dietas, IUnitOfWork unidadDeTrabajo)
    : IHandler<ActualizarDietaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si la dieta no existe.</exception>
    public async Task<Unidad> HandleAsync(ActualizarDietaCommand solicitud, CancellationToken ct)
    {
        var dieta = await dietas.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("dieta", solicitud.Id);

        dieta.Actualizar(solicitud.Nombre.Trim(), solicitud.ProgramaAtencionId);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
