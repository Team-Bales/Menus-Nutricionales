using Menus.Application.Comun;

namespace Menus.Application.GestionDeMenus.ListarMenusDePaciente;

/// <summary>
/// Los menús asignados a un paciente, del más reciente al más antiguo, con su detalle (#84). Quién puede pedirlos lo
/// decide la API: el propio paciente o el personal.
/// </summary>
/// <param name="PacienteId">Identificador del paciente.</param>
public sealed record ListarMenusDePacienteQuery(Guid PacienteId) : IQuery<IReadOnlyList<MenuDetalleDto>>;
