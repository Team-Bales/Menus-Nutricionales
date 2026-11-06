using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Platos.ObtenerPlato;

/// <summary>Busca el plato con su composición y le pone a cada ingrediente el nombre del alimento.</summary>
internal sealed class ObtenerPlatoHandler(IPlatoRepository platos, IAlimentoRepository alimentos)
    : IHandler<ObtenerPlatoQuery, PlatoDetalleDto>
{
    /// <exception cref="NoEncontradoException">Si el plato no existe.</exception>
    public async Task<PlatoDetalleDto> HandleAsync(ObtenerPlatoQuery solicitud, CancellationToken ct)
    {
        var plato = await platos.GetConIngredientesAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("plato", solicitud.Id);

        var nombres = (await alimentos.GetByIdsAsync([.. plato.Ingredientes.Select(i => i.AlimentoId)], ct))
            .ToDictionary(a => a.Id, a => a.Nombre);

        // Todo ingrediente es un alimento existente (RR-02, con FK): el nombre siempre está.
        IReadOnlyList<IngredienteDto> ingredientes =
        [
            .. plato.Ingredientes
                .Select(i => new IngredienteDto(i.AlimentoId, nombres[i.AlimentoId], i.Cantidad))
                .OrderBy(i => i.Nombre, StringComparer.CurrentCulture),
        ];

        return new PlatoDetalleDto(
            plato.Id,
            plato.Nombre,
            plato.GrupoNutricionalId,
            plato.TipoPreparacionId,
            plato.NivelCaloricoId,
            plato.NutricionistaId,
            ingredientes);
    }
}
