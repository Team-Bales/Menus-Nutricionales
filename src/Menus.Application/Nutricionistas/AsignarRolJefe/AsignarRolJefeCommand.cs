using Menus.Application.Comun;

namespace Menus.Application.Nutricionistas.AsignarRolJefe;

/// <summary>Designa a un nutricionista como jefe de nutrición (RD-18, #71).</summary>
/// <param name="NutricionistaId">Identificador del nutricionista.</param>
public sealed record AsignarRolJefeCommand(Guid NutricionistaId) : ICommand<Unidad>;
