using Menus.Application.Comun;

namespace Menus.Application.Pacientes.ActualizarPaciente;

/// <summary>
/// Cambia el nombre, la edad y la sala de un paciente. Sus inscripciones se cambian con sus propios comandos. No
/// hay comando para registrarlo: el alta se hace con la cuenta del paciente, en <c>/api/auth/registro/paciente</c>
/// (#70).
/// </summary>
/// <param name="Id">Identificador del paciente.</param>
/// <param name="Nombre">Nuevo nombre (RD-07).</param>
/// <param name="Edad">Nueva edad en años enteros, mayor o igual que cero (RD-06, RD-09).</param>
/// <param name="SalaId">Sala a la que pertenece; si cambia, el paciente se traslada.</param>
public sealed record ActualizarPacienteCommand(Guid Id, string Nombre, int Edad, Guid SalaId) : ICommand<Unidad>;
