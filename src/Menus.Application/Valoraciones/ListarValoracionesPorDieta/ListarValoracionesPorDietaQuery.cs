using Menus.Application.Comun;

namespace Menus.Application.Valoraciones.ListarValoracionesPorDieta;

/// <summary>
/// Lista las valoraciones de los menús de una dieta, de todos sus pacientes inscritos, de la más a la menos
/// reciente.
/// </summary>
/// <param name="DietaId">Identificador de la dieta.</param>
public sealed record ListarValoracionesPorDietaQuery(Guid DietaId) : IQuery<IReadOnlyList<ValoracionDto>>;
