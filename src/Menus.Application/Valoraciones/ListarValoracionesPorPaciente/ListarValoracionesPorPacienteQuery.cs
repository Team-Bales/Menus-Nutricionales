using Menus.Application.Comun;

namespace Menus.Application.Valoraciones.ListarValoracionesPorPaciente;

/// <summary>Lista las valoraciones de un paciente, de todas sus dietas, de la más a la menos reciente.</summary>
/// <param name="PacienteId">Identificador del paciente.</param>
public sealed record ListarValoracionesPorPacienteQuery(Guid PacienteId) : IQuery<IReadOnlyList<ValoracionDto>>;
