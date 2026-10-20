using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Aprobación de un menú por un nutricionista revisor (relación <c>Valida</c>, con sus atributos).
/// Solo la crea <see cref="Menu.Aprobar"/>, que comprueba las reglas del dictamen.
/// </summary>
/// <remarks>
/// <para>
/// <b>Un menú tiene a lo sumo una validación (R7-32), y por eso su identidad es el menú.</b> El primer dictamen
/// cierra el menú y el rechazo lo elimina (decisión del equipo, 16/10): un menú no llega a tener dos dictámenes.
/// El profesor señaló que el modelo admitía varias validaciones de revisores distintos sin una fecha comparable
/// entre ellas, y dos definiciones de «menú final» (feedback del 19/10). Con una sola validación por menú, final
/// es el menú que la tiene, sin ambigüedad. Si la clave siguiera siendo el par (revisor, menú), no sería mínima:
/// el menú ya basta para identificar la validación.
/// </para>
/// <para>
/// El rechazo no se registra, así que toda validación es una aprobación y <see cref="Aprobado"/> vale siempre
/// <see langword="true"/>; el atributo se conserva porque el MERX lo define (R7-17).
/// </para>
/// </remarks>
public sealed class Validacion : Entity<Guid>
{
    /// <param name="menuId">Identificador del menú validado, que identifica la validación.</param>
    /// <param name="nutricionistaId">Identificador del nutricionista que valida.</param>
    /// <param name="fechaValidacion">Fecha y hora en que se emite el dictamen.</param>
    /// <param name="observaciones">Señalamientos del revisor, si existen.</param>
    internal Validacion(
        Guid menuId,
        Guid nutricionistaId,
        DateTime fechaValidacion,
        string? observaciones = null)
        : base(menuId)
    {
        if (fechaValidacion.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("El instante de validación debe estar en UTC (DateTimeKind.Utc).", nameof(fechaValidacion));
        }

        NutricionistaId = nutricionistaId;
        FechaValidacion = fechaValidacion;
        Aprobado = true;
        Observaciones = observaciones;
    }

    /// <summary>FK → Menú validado, que es también la clave (R7-32).</summary>
    public Guid MenuId => Id;

    /// <summary>FK → Nutricionista revisor.</summary>
    public Guid NutricionistaId { get; }

    /// <summary>Fecha y hora en que se registró la validación.</summary>
    public DateTime FechaValidacion { get; }

    /// <summary>Indica si el dictamen aprobó el menú. Siempre <see langword="true"/>: el rechazo no se registra.</summary>
    public bool Aprobado { get; }

    /// <summary>Observaciones del revisor; puede ser <see langword="null"/>.</summary>
    public string? Observaciones { get; }
}
