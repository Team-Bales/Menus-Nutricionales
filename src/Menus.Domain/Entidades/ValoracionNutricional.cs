using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Valoración nutricional de un paciente sobre un menú que le fue asignado.
/// </summary>
public sealed class ValoracionNutricional : Entity<Guid>
{
    /// <param name="id">Identificador único de la valoración.</param>
    /// <param name="menuId">Identificador del menú de la asignación evaluada.</param>
    /// <param name="pacienteId">Identificador del paciente de la asignación evaluada.</param>
    /// <param name="nutricionistaId">Identificador del nutricionista que emite la valoración.</param>
    /// <param name="resultadoNutricionalId">Identificador de la categoría de resultado nutricional.</param>
    public ValoracionNutricional(
        Guid id,
        Guid menuId,
        Guid pacienteId,
        Guid nutricionistaId,
        Guid resultadoNutricionalId)
        : base(id)
    {
        MenuId = menuId;
        PacienteId = pacienteId;
        NutricionistaId = nutricionistaId;
        ResultadoNutricionalId = resultadoNutricionalId;
    }

    /// <summary>FK → Menú de la asignación evaluada.</summary>
    public Guid MenuId { get; }

    /// <summary>FK → Paciente de la asignación evaluada.</summary>
    public Guid PacienteId { get; }

    /// <summary>FK → Nutricionista que emite la valoración.</summary>
    public Guid NutricionistaId { get; }

    /// <summary>FK → Categoría de resultado nutricional.</summary>
    public Guid ResultadoNutricionalId { get; }
}
