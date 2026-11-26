using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Fichas.ObtenerFicha;

/// <summary>Consulta la ficha nutricional de un alimento o un plato (#283).</summary>
/// <param name="Clase">Clase del ítem, según la ruta.</param>
/// <param name="ItemId">El alimento o el plato.</param>
public sealed record ObtenerFichaQuery(ClaseDeItem Clase, Guid ItemId) : IQuery<FichaNutricionalDto>;
