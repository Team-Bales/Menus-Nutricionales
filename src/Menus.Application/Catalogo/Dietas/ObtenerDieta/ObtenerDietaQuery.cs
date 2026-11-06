using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Dietas.ObtenerDieta;

/// <summary>Detalle de una dieta, con sus grupos y restricciones expandidos.</summary>
/// <param name="Id">Identificador de la dieta.</param>
public sealed record ObtenerDietaQuery(Guid Id) : IQuery<DietaDto>;
