using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Solicitud de reevaluación de una valoración nutricional por un paciente.
/// Puede generar, posteriormente, una nueva valoración nutricional.
/// </summary>
public sealed class Revalorizacion : Entity<Guid>
{
    /// <param name="id">Identificador único de la revalorización.</param>
    /// <param name="fechaSolicitud">Fecha y hora en que se solicita la revalorización.</param>
    /// <param name="pacienteId">Identificador del paciente que solicita la revalorización.</param>
    /// <param name="nutricionistaId">Identificador del nutricionista que la ejecuta.</param>
    /// <param name="valoracionRevisadaId">Identificador de la valoración que se revisa.</param>
    public Revalorizacion(
        Guid id,
        DateTime fechaSolicitud,
        Guid pacienteId,
        Guid nutricionistaId,
        Guid valoracionRevisadaId)
        : base(id)
    {
        if (fechaSolicitud.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("El instante de solicitud debe estar en UTC (DateTimeKind.Utc).", nameof(fechaSolicitud));
        }

        FechaSolicitud = fechaSolicitud;
        PacienteId = pacienteId;
        NutricionistaId = nutricionistaId;
        ValoracionRevisadaId = valoracionRevisadaId;
    }

    /// <summary>Fecha y hora en que el paciente solicitó la revalorización.</summary>
    public DateTime FechaSolicitud { get; }

    /// <summary>FK → Paciente que solicita la revalorización.</summary>
    public Guid PacienteId { get; }

    /// <summary>FK → Nutricionista que ejecuta la revalorización.</summary>
    public Guid NutricionistaId { get; }

    /// <summary>FK → Valoración nutricional que se revisa.</summary>
    public Guid ValoracionRevisadaId { get; }

    /// <summary>
    /// FK → Valoración nutricional generada al completar la revalorización; es
    /// <see langword="null"/> mientras la solicitud está pendiente.
    /// </summary>
    public Guid? ValoracionGeneradaId { get; private set; }

    /// <summary>Registra la valoración nutricional generada al completar la revalorización.</summary>
    /// <param name="valoracionGeneradaId">Identificador de la valoración generada.</param>
    /// <exception cref="InvalidOperationException">Si la revalorización ya generó una valoración.</exception>
    public void RegistrarValoracionGenerada(Guid valoracionGeneradaId)
    {
        if (ValoracionGeneradaId is not null)
        {
            throw new InvalidOperationException("La revalorización ya tiene una valoración generada.");
        }

        ValoracionGeneradaId = valoracionGeneradaId;
    }
}
