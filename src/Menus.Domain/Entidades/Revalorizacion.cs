using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Solicitud de reevaluación de una valoración nutricional por el paciente valorado.
/// Puede generar, posteriormente, una nueva valoración nutricional.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por qué no guarda el paciente solicitante.</b> El solicitante es siempre el paciente de la valoración que se
/// revisa (R7-29), así que se deduce de ella: valoración revisada → asignación → paciente. Si se guardara, valdría
/// la dependencia valoración revisada → paciente, y como la valoración revisada no es clave (una valoración puede
/// revisarse varias veces), sería una dependencia transitiva: la tabla no estaría en 3FN. El #12 (§2.3) la había
/// dejado así como redundancia controlada; el profesor exige al menos la 3FN (feedback del 19/10), así que se adoptó
/// la otra opción del #12: quitar el paciente, con lo que la tabla queda en BCNF y R7-29 se cumple por estructura.
/// </para>
/// <para>
/// Que quien solicita sea de verdad el paciente valorado (y no otro usuario) es una comprobación de autorización
/// que hace la capa de aplicación al recibir la solicitud.
/// </para>
/// </remarks>
public sealed class Revalorizacion : Entity<Guid>
{
    /// <param name="id">Identificador único de la revalorización.</param>
    /// <param name="fechaSolicitud">Fecha y hora en que se solicita la revalorización.</param>
    /// <param name="nutricionistaId">Identificador del nutricionista que la ejecuta.</param>
    /// <param name="valoracionRevisadaId">
    /// Identificador de la valoración que se revisa; su paciente es el solicitante (R7-29).
    /// </param>
    public Revalorizacion(
        Guid id,
        DateTime fechaSolicitud,
        Guid nutricionistaId,
        Guid valoracionRevisadaId)
        : base(id)
    {
        if (fechaSolicitud.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("El instante de solicitud debe estar en UTC (DateTimeKind.Utc).", nameof(fechaSolicitud));
        }

        FechaSolicitud = fechaSolicitud;
        NutricionistaId = nutricionistaId;
        ValoracionRevisadaId = valoracionRevisadaId;
    }

    /// <summary>Fecha y hora en que el paciente solicitó la revalorización.</summary>
    public DateTime FechaSolicitud { get; }

    /// <summary>FK → Nutricionista que ejecuta la revalorización.</summary>
    public Guid NutricionistaId { get; }

    /// <summary>FK → Valoración nutricional que se revisa. Su paciente es el que solicita la revalorización.</summary>
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
