using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Entidad de unión que representa la inscripción de un paciente en una dieta
/// (relación "Inscribe": Paciente ↔ Dieta, N:M).
/// </summary>
public sealed class PacienteDieta : Entity<(Guid PacienteId, Guid DietaId)>
{
    /// <param name="pacienteId">Identificador del paciente inscrito.</param>
    /// <param name="dietaId">Identificador de la dieta en la que se inscribe.</param>
    public PacienteDieta(Guid pacienteId, Guid dietaId)
        : base((pacienteId, dietaId))
    {
    }

    /// <summary>FK → Paciente (parte de la clave compuesta).</summary>
    public Guid PacienteId => Id.PacienteId;

    /// <summary>FK → Dieta (parte de la clave compuesta).</summary>
    public Guid DietaId => Id.DietaId;
}
