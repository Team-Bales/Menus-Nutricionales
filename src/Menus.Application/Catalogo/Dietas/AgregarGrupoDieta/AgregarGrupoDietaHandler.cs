using Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;
using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Dietas.AgregarGrupoDieta;

/// <summary>
/// Agrega el grupo a la cobertura de la dieta, o no hace nada si ya la cubre (idempotente). Invalida su
/// disponibilidad calórica (informe, «Consultas recurrentes»; #101).
/// </summary>
internal sealed class AgregarGrupoDietaHandler(IDietaRepository dietas, IUnitOfWork unidadDeTrabajo, ICache cache)
    : IHandler<AgregarGrupoDietaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si la dieta no existe.</exception>
    public async Task<Unidad> HandleAsync(AgregarGrupoDietaCommand solicitud, CancellationToken ct)
    {
        var dieta = await dietas.GetConGruposYRestriccionesAsync(solicitud.DietaId, ct)
            ?? throw new NoEncontradoException("dieta", solicitud.DietaId);

        // Idempotente: una segunda llamada con el mismo grupo no falla ni duplica la cobertura (criterio del #63).
        // Si ya la cubre con otro tipo, se deja como está: cambiar el tipo es una decisión explícita, no esta.
        if (!dieta.GruposNutricionales.Any(g => g.GrupoNutricionalId == solicitud.GrupoNutricionalId))
        {
            dieta.AgregarGrupo(solicitud.GrupoNutricionalId, solicitud.Tipo);
            await unidadDeTrabajo.CommitAsync(ct);
            await CacheDeDisponibilidadCalorica.InvalidarAsync(cache, solicitud.DietaId, ct);
        }

        return Unidad.Valor;
    }
}
