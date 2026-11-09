using Menus.Application.Comun;

namespace Menus.Application.Pacientes.DesinscribirDieta;

/// <summary>
/// Retira la inscripción de un paciente en una dieta. Las asignaciones de menús ya hechas no cambian (ver
/// <see cref="Domain.Entidades.Paciente.DesinscribirDieta"/>).
/// </summary>
/// <param name="PacienteId">Identificador del paciente.</param>
/// <param name="DietaId">Identificador de la dieta.</param>
public sealed record DesinscribirDietaCommand(Guid PacienteId, Guid DietaId) : ICommand<Unidad>;
