using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.Catalogo.Platos.ActualizarPlato;

/// <summary>Reclasifica el plato en nombre del actor.</summary>
internal sealed class ActualizarPlatoHandler(
    IPlatoRepository platos,
    IUnitOfWork unidadDeTrabajo,
    IActorActual actor) : IHandler<ActualizarPlatoCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el plato no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si el actor no es quien describió el plato (RS-02).</exception>
    public async Task<Unidad> HandleAsync(ActualizarPlatoCommand solicitud, CancellationToken ct)
    {
        var nutricionistaId = actor.Id;
        var plato = await platos.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("plato", solicitud.Id);

        plato.Reclasificar(
            nutricionistaId,
            solicitud.GrupoNutricionalId,
            solicitud.TipoPreparacionId,
            solicitud.NivelCaloricoId);

        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
