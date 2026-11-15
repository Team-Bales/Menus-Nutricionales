using Menus.Application.Comun;

namespace Menus.Application.GestionDeMenus.AsignarMenu;

/// <summary>
/// Asigna un menú final a un paciente (agregación <c>Asignación</c>, #84). Un menú se asigna a varios pacientes, una
/// sola vez a cada uno (RC-13).
/// </summary>
/// <param name="MenuId">Identificador del menú.</param>
/// <param name="PacienteId">Identificador del paciente.</param>
public sealed record AsignarMenuCommand(Guid MenuId, Guid PacienteId) : ICommand<Unidad>;
