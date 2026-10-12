using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Asignación de un menú a un paciente (agregación <c>Asignación</c> / relación <c>Asigna</c>).
/// Un menú puede asignarse a varios pacientes, pero una sola vez a cada uno (R7-05).
/// De la asignación dependen los consumos y las valoraciones del paciente ante ese menú (R7-11, R7-12).
/// Solo la crea <see cref="Menu"/>.
/// </summary>
public sealed class Asignacion : Entity<(Guid MenuId, Guid PacienteId)>
{
    /// <param name="menuId">Identificador del menú asignado.</param>
    /// <param name="pacienteId">Identificador del paciente que recibe el menú.</param>
    internal Asignacion(Guid menuId, Guid pacienteId) : base((menuId, pacienteId)) { }

    /// <summary>FK → Menu (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>FK → Paciente (parte de la clave compuesta).</summary>
    public Guid PacienteId => Id.PacienteId;
}
