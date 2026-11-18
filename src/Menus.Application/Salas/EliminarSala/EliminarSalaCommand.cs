using Menus.Application.Comun;

namespace Menus.Application.Salas.EliminarSala;

/// <summary>Elimina una sala, solo si no tiene pacientes asignados (#72).</summary>
/// <param name="Id">Identificador de la sala.</param>
public sealed record EliminarSalaCommand(Guid Id) : ICommand<Unidad>;
