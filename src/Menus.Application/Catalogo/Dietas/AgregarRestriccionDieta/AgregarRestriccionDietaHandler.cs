using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Dietas.AgregarRestriccionDieta;

/// <summary>Agrega la restricción a la dieta, o no hace nada si ya la tiene (idempotente).</summary>
internal sealed class AgregarRestriccionDietaHandler(IDietaRepository dietas, IUnitOfWork unidadDeTrabajo)
    : IHandler<AgregarRestriccionDietaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si la dieta no existe.</exception>
    public async Task<Unidad> HandleAsync(AgregarRestriccionDietaCommand solicitud, CancellationToken ct)
    {
        var dieta = await dietas.GetConGruposYRestriccionesAsync(solicitud.DietaId, ct)
            ?? throw new NoEncontradoException("dieta", solicitud.DietaId);

        if (!dieta.Restricciones.Any(r => r.RestriccionAlimentariaId == solicitud.RestriccionAlimentariaId))
        {
            dieta.AgregarRestriccion(solicitud.RestriccionAlimentariaId);
            await unidadDeTrabajo.CommitAsync(ct);
        }

        return Unidad.Valor;
    }
}
