using Menus.Domain.Entidades;

namespace Menus.Application.Pacientes;

/// <summary>Un paciente, sin su sala ni sus dietas expandidas: la forma que usa el listado.</summary>
/// <param name="Id">Identificador del paciente.</param>
/// <param name="Nombre">Nombre del paciente.</param>
/// <param name="Edad">Edad en años enteros (RD-06, RD-09).</param>
/// <param name="SalaId">Sala de hospitalización a la que pertenece.</param>
public sealed record PacienteDto(Guid Id, string Nombre, int Edad, Guid SalaId)
{
    /// <summary>Copia los datos de la entidad.</summary>
    /// <param name="paciente">Paciente del dominio.</param>
    internal static PacienteDto Desde(Paciente paciente) => new(paciente.Id, paciente.Nombre, paciente.Edad, paciente.SalaId);
}

/// <summary>La sala de un paciente, con su nombre.</summary>
/// <param name="Id">Identificador de la sala.</param>
/// <param name="Nombre">Nombre de la sala.</param>
public sealed record SalaDelPacienteDto(Guid Id, string Nombre);

/// <summary>Una dieta en la que está inscrito un paciente, con su nombre.</summary>
/// <param name="Id">Identificador de la dieta.</param>
/// <param name="Nombre">Nombre de la dieta.</param>
public sealed record DietaInscritaDto(Guid Id, string Nombre);

/// <summary>Un paciente con su sala y sus dietas inscritas expandidas: la forma que usa el detalle.</summary>
/// <param name="Id">Identificador del paciente.</param>
/// <param name="Nombre">Nombre del paciente.</param>
/// <param name="Edad">Edad en años enteros (RD-06, RD-09).</param>
/// <param name="Sala">Sala de hospitalización a la que pertenece.</param>
/// <param name="Dietas">Dietas en las que está inscrito, ordenadas por nombre.</param>
public sealed record PacienteDetalleDto(
    Guid Id,
    string Nombre,
    int Edad,
    SalaDelPacienteDto Sala,
    IReadOnlyList<DietaInscritaDto> Dietas);
