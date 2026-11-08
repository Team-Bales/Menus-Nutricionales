using Menus.Application.Comun;

namespace Menus.Application.Pacientes.ObtenerPaciente;

/// <summary>Detalle de un paciente, con su sala y sus dietas inscritas.</summary>
/// <param name="Id">Identificador del paciente.</param>
public sealed record ObtenerPacienteQuery(Guid Id) : IQuery<PacienteDetalleDto>;
