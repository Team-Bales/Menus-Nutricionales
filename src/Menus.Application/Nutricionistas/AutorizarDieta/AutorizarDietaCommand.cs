using Menus.Application.Comun;

namespace Menus.Application.Nutricionistas.AutorizarDieta;

/// <summary>Autoriza a un nutricionista para crear o validar menús de una dieta (relación <c>Autoriza</c>, #71).</summary>
/// <param name="NutricionistaId">Identificador del nutricionista.</param>
/// <param name="DietaId">Identificador de la dieta a autorizar.</param>
public sealed record AutorizarDietaCommand(Guid NutricionistaId, Guid DietaId) : ICommand<Unidad>;
