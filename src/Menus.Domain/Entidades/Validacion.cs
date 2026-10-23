using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Aprobación de un menú por un nutricionista revisor (agregación <c>Validación</c>).
/// Solo la crea <see cref="Menu.Aprobar"/>, que comprueba las reglas del dictamen.
/// </summary>
/// <remarks>
/// El rechazo no se registra: elimina el menú (decisión del equipo, 16/10). Por eso toda validación es una
/// aprobación y <see cref="Aprobado"/> vale siempre <see langword="true"/>; el atributo se conserva porque
/// el MERX lo define (R7-17).
/// </remarks>
public sealed class Validacion : Entity<(Guid NutricionistaId, Guid MenuId)>
{
    /// <param name="nutricionistaId">Identificador del nutricionista que valida.</param>
    /// <param name="menuId">Identificador del menú validado.</param>
    /// <param name="fechaValidacion">Fecha y hora en que se emite el dictamen.</param>
    /// <param name="observaciones">Señalamientos del revisor, si existen.</param>
    internal Validacion(
        Guid nutricionistaId,
        Guid menuId,
        DateTime fechaValidacion,
        string? observaciones = null)
        : base((nutricionistaId, menuId))
    {
        if (fechaValidacion.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("El instante de validación debe estar en UTC (DateTimeKind.Utc).", nameof(fechaValidacion));
        }

        FechaValidacion = fechaValidacion;
        Aprobado = true;
        Observaciones = observaciones;
    }

    /// <summary>FK → Nutricionista revisor (parte de la clave compuesta).</summary>
    public Guid NutricionistaId => Id.NutricionistaId;

    /// <summary>FK → Menú validado (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>Fecha y hora en que se registró la validación.</summary>
    public DateTime FechaValidacion { get; }

    /// <summary>Indica si el dictamen aprobó el menú. Siempre <see langword="true"/>: el rechazo no se registra.</summary>
    public bool Aprobado { get; }

    /// <summary>Observaciones del revisor; puede ser <see langword="null"/>.</summary>
    public string? Observaciones { get; }
}
