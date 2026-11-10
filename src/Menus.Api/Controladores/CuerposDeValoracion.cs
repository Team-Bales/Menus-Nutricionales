namespace Menus.Api.Controladores;

/// <summary>Cuerpo de <c>POST /api/valoraciones</c>: la asignación evaluada y el resultado.</summary>
/// <param name="MenuId">Identificador del menú de la asignación evaluada.</param>
/// <param name="PacienteId">Identificador del paciente de la asignación evaluada.</param>
/// <param name="ResultadoNutricionalId">Identificador de una de las categorías de resultado nutricional.</param>
public sealed record CuerpoDeValoracion(Guid MenuId, Guid PacienteId, Guid ResultadoNutricionalId);
