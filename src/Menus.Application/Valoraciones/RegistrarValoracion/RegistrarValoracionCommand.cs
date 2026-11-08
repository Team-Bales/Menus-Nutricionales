using Menus.Application.Comun;

namespace Menus.Application.Valoraciones.RegistrarValoracion;

/// <summary>
/// Registra la valoración nutricional de un nutricionista sobre la asignación de un menú a un paciente
/// (relación <c>Evalúa</c>). El nutricionista que la emite y el instante los pone el manejador, nunca el cliente.
/// </summary>
/// <param name="MenuId">Identificador del menú de la asignación evaluada.</param>
/// <param name="PacienteId">Identificador del paciente de la asignación evaluada.</param>
/// <param name="ResultadoNutricionalId">Identificador de una de las categorías de <c>ResultadoNutricional</c>.</param>
public sealed record RegistrarValoracionCommand(
    Guid MenuId,
    Guid PacienteId,
    Guid ResultadoNutricionalId) : ICommand<IdCreadoDto>;
