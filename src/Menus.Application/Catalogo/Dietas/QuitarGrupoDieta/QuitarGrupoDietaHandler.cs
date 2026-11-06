using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Dietas.QuitarGrupoDieta;

/// <summary>Quita el grupo de la cobertura de la dieta, o no hace nada si no la cubre (idempotente).</summary>
internal sealed class QuitarGrupoDietaHandler(IDietaRepository dietas, IUnitOfWork unidadDeTrabajo)
    : IHandler<QuitarGrupoDietaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si la dieta no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si es el único grupo requerido de la dieta (RK-07).</exception>
    public async Task<Unidad> HandleAsync(QuitarGrupoDietaCommand solicitud, CancellationToken ct)
    {
        var dieta = await dietas.GetConGruposYRestriccionesAsync(solicitud.DietaId, ct)
            ?? throw new NoEncontradoException("dieta", solicitud.DietaId);

        if (dieta.GruposNutricionales.Any(g => g.GrupoNutricionalId == solicitud.GrupoNutricionalId))
        {
            dieta.QuitarGrupo(solicitud.GrupoNutricionalId);
            await unidadDeTrabajo.CommitAsync(ct);
        }

        return Unidad.Valor;
    }
}
