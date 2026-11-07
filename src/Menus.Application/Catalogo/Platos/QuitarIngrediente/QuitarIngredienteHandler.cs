using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.Catalogo.Platos.QuitarIngrediente;

/// <summary>Quita el ingrediente en nombre del actor, que tiene que ser quien describió el plato.</summary>
internal sealed class QuitarIngredienteHandler(
    IPlatoRepository platos,
    IUnitOfWork unidadDeTrabajo,
    IActorActual actor) : IHandler<QuitarIngredienteCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el plato no existe, o si el alimento no está en su composición.</exception>
    /// <exception cref="ReglaDeNegocioException">
    /// Si el actor no describió el plato (RS-02), o si es su único ingrediente (RK-03).
    /// </exception>
    public async Task<Unidad> HandleAsync(QuitarIngredienteCommand solicitud, CancellationToken ct)
    {
        var nutricionistaId = actor.Id;
        var plato = await platos.GetConIngredientesAsync(solicitud.PlatoId, ct)
            ?? throw new NoEncontradoException("plato", solicitud.PlatoId);

        ComposicionDelPlato.ExigirDescriptor(plato, nutricionistaId);

        // La ruta nombra el ingrediente (DELETE …/ingredientes/{alimentoId}): si no está, el recurso no existe.
        if (!plato.Ingredientes.Any(i => i.AlimentoId == solicitud.AlimentoId))
        {
            throw new NoEncontradoException("ingrediente", solicitud.AlimentoId);
        }

        plato.QuitarIngrediente(solicitud.AlimentoId);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
