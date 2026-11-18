using Menus.Application.Comun;

namespace Menus.Application.Nutricionistas.ActualizarNutricionista;

/// <summary>Actualiza el nombre y la especialidad de un nutricionista (#71).</summary>
/// <param name="Id">Identificador del nutricionista.</param>
/// <param name="Nombre">Nuevo nombre.</param>
/// <param name="EspecialidadId">Nueva especialidad.</param>
public sealed record ActualizarNutricionistaCommand(Guid Id, string Nombre, Guid EspecialidadId) : ICommand<Unidad>;
