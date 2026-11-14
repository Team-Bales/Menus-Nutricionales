using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.ValueObjects;

namespace Menus.Application.GeneracionDeMenus.GenerarMenuAutomaticamente;

/// <summary>
/// Genera automáticamente un menú para una dieta (#98; informe, RF-10), delegando la selección de ítems en
/// <see cref="IEstrategiaGeneracionMenu"/> (patrón <i>Strategy</i>, checklist #96).
/// </summary>
internal sealed class GenerarMenuAutomaticamenteHandler(
    IDietaRepository dietas,
    IItemAlimentarioRepository items,
    IMenuRepository menus,
    INutricionistaRepository nutricionistas,
    IUnitOfWork unidadDeTrabajo,
    IActorActual actor,
    IEstrategiaGeneracionMenu estrategia) : IHandler<GenerarMenuAutomaticamenteCommand, IdCreadoDto>
{
    /// <exception cref="NoEncontradoException">Si la dieta no existe, o si el actor no es un nutricionista existente.</exception>
    /// <exception cref="ReglaDeNegocioException">
    /// Si el nutricionista no está autorizado para la dieta (RF-06, RS-03), o si no hay suficientes ítems
    /// compatibles para cumplir la distribución pedida.
    /// </exception>
    public async Task<IdCreadoDto> HandleAsync(GenerarMenuAutomaticamenteCommand solicitud, CancellationToken ct)
    {
        _ = await dietas.GetByIdAsync(solicitud.DietaId, ct)
            ?? throw new NoEncontradoException("dieta", solicitud.DietaId);

        var nutricionistaId = actor.Id;
        var nutricionista = await nutricionistas.GetByIdAsync(nutricionistaId, ct)
            ?? throw new NoEncontradoException("nutricionista", nutricionistaId);

        if (nutricionista.DietasAutorizadas.All(a => a.DietaId != solicitud.DietaId))
        {
            throw new ReglaDeNegocioException(
                $"El nutricionista {nutricionistaId} no está autorizado para la dieta {solicitud.DietaId} (RF-06).",
                "RS-03");
        }

        var cantidadTotal = CantidadTotalAlimentos.Crear(solicitud.CantidadTotalAlimentos);
        var distribucion = DistribucionTiposPreparacion.CrearDesdeTuplas(solicitud.Distribucion);
        var cobertura = CoberturaGruposNutricionales.Crear(solicitud.GruposNutricionalesIds);

        var gruposProhibidos = new HashSet<Guid>(await dietas.GetGruposProhibidosAsync(solicitud.DietaId, ct));
        var todosLosItems = await items.ListAsync(ct);
        var itemsCompatibles = todosLosItems.Where(i => !gruposProhibidos.Contains(i.GrupoNutricionalId)).ToList();

        var idsSeleccionados = estrategia.SeleccionarItems(itemsCompatibles, distribucion, cobertura, cantidadTotal);

        var menu = new Menu(
            Guid.NewGuid(),
            solicitud.DietaId,
            nutricionistaId,
            DateTime.UtcNow,
            cantidadTotal,
            esAutomatico: true,
            distribucion,
            cobertura);

        foreach (var itemId in idsSeleccionados)
        {
            menu.AgregarItem(itemId);
        }

        await menus.AddAsync(menu, ct);
        await unidadDeTrabajo.CommitAsync(ct);

        return new IdCreadoDto(menu.Id);
    }
}
