using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Platos.ObtenerPlato;

/// <summary>Detalle de un plato con sus ingredientes.</summary>
/// <param name="Id">Identificador del plato.</param>
public sealed record ObtenerPlatoQuery(Guid Id) : IQuery<PlatoDetalleDto>;
