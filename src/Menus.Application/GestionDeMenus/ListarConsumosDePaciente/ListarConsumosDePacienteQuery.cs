using Menus.Application.Comun;

namespace Menus.Application.GestionDeMenus.ListarConsumosDePaciente;

/// <summary>
/// El histórico de consumo de un paciente: cada servicio de sus menús asignados con sus consumos, del día más
/// reciente al más antiguo (#85). Quién puede pedirlo lo decide la API: el propio paciente o el personal.
/// </summary>
/// <param name="PacienteId">Identificador del paciente.</param>
public sealed record ListarConsumosDePacienteQuery(Guid PacienteId) : IQuery<IReadOnlyList<ServicioDto>>;
