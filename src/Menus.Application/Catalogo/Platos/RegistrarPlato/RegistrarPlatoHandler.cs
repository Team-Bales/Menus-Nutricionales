using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;

namespace Menus.Application.Catalogo.Platos.RegistrarPlato;

/// <summary>Registra el plato con su composición en una sola transacción y devuelve su identificador.</summary>
/// <remarks>
/// El plato y sus ingredientes se confirman juntos: RK-03 es un trigger diferido que rechaza un plato sin alimentos al
/// confirmar (ADR-010, §5).
/// </remarks>
internal sealed class RegistrarPlatoHandler(
    IItemAlimentarioRepository items,
    IAlimentoRepository alimentos,
    IPlatoRepository platos,
    IUnitOfWork unidadDeTrabajo,
    IActorActual actor) : IHandler<RegistrarPlatoCommand, IdCreadoDto>
{
    /// <exception cref="ReglaDeNegocioException">
    /// Si ya hay un ítem con ese nombre (RC-01), o si un ingrediente no existe (RR-02) o es un plato (RS-12).
    /// </exception>
    public async Task<IdCreadoDto> HandleAsync(RegistrarPlatoCommand solicitud, CancellationToken ct)
    {
        var nutricionistaId = actor.Id;
        var nombre = solicitud.Nombre.Trim();

        if (await items.ExisteNombreAsync(nombre, ct))
        {
            throw new ReglaDeNegocioException($"Ya existe un alimento o un plato llamado «{nombre}».", "RC-01");
        }

        await ComposicionDelPlato.ExigirAlimentosAsync(
            [.. solicitud.Ingredientes.Select(i => i.AlimentoId)], alimentos, items, ct);

        var plato = new Plato(
            Guid.NewGuid(),
            nombre,
            solicitud.GrupoNutricionalId,
            solicitud.TipoPreparacionId,
            solicitud.NivelCaloricoId,
            nutricionistaId);

        foreach (var ingrediente in solicitud.Ingredientes)
        {
            plato.AgregarIngrediente(ingrediente.AlimentoId, ingrediente.Cantidad);
        }

        await platos.AddAsync(plato, ct);
        await unidadDeTrabajo.CommitAsync(ct);

        return new IdCreadoDto(plato.Id);
    }
}
