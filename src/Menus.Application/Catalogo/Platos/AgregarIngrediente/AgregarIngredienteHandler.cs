using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.Catalogo.Platos.AgregarIngrediente;

/// <summary>
/// Agrega el ingrediente en nombre del actor, que tiene que ser quien describió el plato, y después lo agrega al
/// desglose de la ficha del plato (R8-06).
/// </summary>
internal sealed class AgregarIngredienteHandler(
    IItemAlimentarioRepository items,
    IAlimentoRepository alimentos,
    IPlatoRepository platos,
    IUnitOfWork unidadDeTrabajo,
    IActorActual actor,
    IFichasNutricionales fichas) : IHandler<AgregarIngredienteCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el plato no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">
    /// Si el actor no describió el plato (RS-02), si el alimento no existe (RR-02) o es un plato (RS-12), o si ya está
    /// en la composición.
    /// </exception>
    public async Task<Unidad> HandleAsync(AgregarIngredienteCommand solicitud, CancellationToken ct)
    {
        var nutricionistaId = actor.Id;
        var plato = await platos.GetConIngredientesAsync(solicitud.PlatoId, ct)
            ?? throw new NoEncontradoException("plato", solicitud.PlatoId);

        ComposicionDelPlato.ExigirDescriptor(plato, nutricionistaId);
        var alimento = (await ComposicionDelPlato.ExigirAlimentosAsync([solicitud.AlimentoId], alimentos, items, ct))[0];

        plato.AgregarIngrediente(alimento.Id, solicitud.Cantidad);
        await unidadDeTrabajo.CommitAsync(ct);
        await fichas.AgregarIngredienteAsync(plato.Id, alimento);

        return Unidad.Valor;
    }
}
