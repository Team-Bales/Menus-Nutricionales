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
    /// <param name="fechaValoracion">Fecha y hora en que se emite la valoración (UTC).</param>
    /// <param name="observaciones">Notas del nutricionista; puede ser <see langword="null"/>.</param>
    /// <exception cref="ArgumentException">Si <paramref name="fechaValoracion"/> no está en UTC.</exception>
    public ValoracionNutricional(
        Guid id,
        Guid menuId,
        Guid pacienteId,
        Guid nutricionistaId,
        Guid resultadoNutricionalId,
        DateTime fechaValoracion,
        string? observaciones = null)
        : base(id)
    {
        if (fechaValoracion.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("El instante de valoración debe estar en UTC (DateTimeKind.Utc).", nameof(fechaValoracion));
        }

        MenuId = menuId;
        PacienteId = pacienteId;
        NutricionistaId = nutricionistaId;
        ResultadoNutricionalId = resultadoNutricionalId;
        FechaValoracion = fechaValoracion;
        Observaciones = observaciones;
    }

    /// <summary>FK → Menú de la asignación evaluada.</summary>
    public Guid MenuId { get; }

    /// <summary>FK → Paciente de la asignación evaluada.</summary>
    public Guid PacienteId { get; }

    /// <summary>FK → Nutricionista que emite la valoración.</summary>
    public Guid NutricionistaId { get; }

    /// <summary>FK → Categoría de resultado nutricional.</summary>
    public Guid ResultadoNutricionalId { get; }

    /// <summary>Fecha y hora en que se emitió la valoración.</summary>
    public DateTime FechaValoracion { get; }

    /// <summary>Observaciones del nutricionista; puede ser <see langword="null"/>.</summary>
    public string? Observaciones { get; }
}
