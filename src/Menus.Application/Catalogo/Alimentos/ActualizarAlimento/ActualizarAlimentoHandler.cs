using Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;
using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.Catalogo.Alimentos.ActualizarAlimento;

/// <summary>
/// Reclasifica el alimento en nombre del actor e invalida la disponibilidad calórica de todas las dietas
/// (informe, «Consultas recurrentes»; #101): cambiar el grupo o el nivel calórico la cambia.
/// </summary>
internal sealed class ActualizarAlimentoHandler(
    IAlimentoRepository alimentos,
    IDietaRepository dietas,
    IUnitOfWork unidadDeTrabajo,
    IActorActual actor,
    ICache cache) : IHandler<ActualizarAlimentoCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el alimento no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si el actor no es quien describió el alimento (RS-02).</exception>
    public async Task<Unidad> HandleAsync(ActualizarAlimentoCommand solicitud, CancellationToken ct)
    {
        var nutricionistaId = actor.Id;
        var alimento = await alimentos.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("alimento", solicitud.Id);

        alimento.Reclasificar(
            nutricionistaId,
            solicitud.GrupoNutricionalId,
            solicitud.TipoPreparacionId,
            solicitud.NivelCaloricoId);

        await unidadDeTrabajo.CommitAsync(ct);

        await CacheDeDisponibilidadCalorica.InvalidarTodasAsync(dietas, cache, ct);

        return Unidad.Valor;
    }
}
