using Menus.Application.Comun;

namespace Menus.Application.Nutricionistas.ObtenerNutricionista;

/// <summary>Detalle de un nutricionista, con sus dietas autorizadas (#71).</summary>
/// <param name="Id">Identificador del nutricionista.</param>
public sealed record ObtenerNutricionistaQuery(Guid Id) : IQuery<NutricionistaDetalleDto>;
