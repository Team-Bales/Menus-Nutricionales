using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.Catalogo.Alimentos.ActualizarAlimento;

/// <summary>Reclasifica el alimento en nombre del actor.</summary>
internal sealed class ActualizarAlimentoHandler(
    IAlimentoRepository alimentos,
    IUnitOfWork unidadDeTrabajo,
    IActorActual actor) : IHandler<ActualizarAlimentoCommand, Unidad>
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

        return Unidad.Valor;
    }
}
