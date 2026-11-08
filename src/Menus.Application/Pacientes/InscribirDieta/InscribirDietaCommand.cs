using Menus.Application.Comun;

namespace Menus.Application.Pacientes.InscribirDieta;

/// <summary>
/// Inscribe a un paciente en una dieta (relación <c>Inscribe</c>). Si ya está inscrito, no hace nada (idempotente):
/// no falla ni duplica la inscripción (RC-07).
/// </summary>
/// <param name="PacienteId">Identificador del paciente.</param>
/// <param name="DietaId">Identificador de la dieta.</param>
public sealed record InscribirDietaCommand(Guid PacienteId, Guid DietaId) : ICommand<Unidad>;
