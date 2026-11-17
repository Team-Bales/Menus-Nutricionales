using Menus.Application.Comun;

namespace Menus.Application.Nutricionistas.RevocarDieta;

/// <summary>Revoca la autorización de un nutricionista para una dieta (#71).</summary>
/// <param name="NutricionistaId">Identificador del nutricionista.</param>
/// <param name="DietaId">Identificador de la dieta cuya autorización se revoca.</param>
public sealed record RevocarDietaCommand(Guid NutricionistaId, Guid DietaId) : ICommand<Unidad>;
