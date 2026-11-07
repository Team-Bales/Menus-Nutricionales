using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Dietas.QuitarRestriccionDieta;

/// <summary>Quita la restricción de la dieta, o no hace nada si no la tiene (idempotente).</summary>
internal sealed class QuitarRestriccionDietaHandler(IDietaRepository dietas, IUnitOfWork unidadDeTrabajo)
    : IHandler<QuitarRestriccionDietaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si la dieta no existe.</exception>
    public async Task<Unidad> HandleAsync(QuitarRestriccionDietaCommand solicitud, CancellationToken ct)
    {
        var dieta = await dietas.GetConGruposYRestriccionesAsync(solicitud.DietaId, ct)
            ?? throw new NoEncontradoException("dieta", solicitud.DietaId);

        if (dieta.Restricciones.Any(r => r.RestriccionAlimentariaId == solicitud.RestriccionAlimentariaId))
        {
            dieta.QuitarRestriccion(solicitud.RestriccionAlimentariaId);
            await unidadDeTrabajo.CommitAsync(ct);
        }

        return Unidad.Valor;
    }
}
