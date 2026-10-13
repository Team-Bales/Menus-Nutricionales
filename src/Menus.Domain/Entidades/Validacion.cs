using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Dictamen de un nutricionista sobre un menú.
/// Cada nutricionista puede registrar una única validación por menú.
/// </summary>
public sealed class Validacion : Entity<(Guid NutricionistaId, Guid MenuId)>
{
    /// <param name="nutricionistaId">Identificador del nutricionista que valida.</param>
    /// <param name="menuId">Identificador del menú validado.</param>
    /// <param name="fechaValidacion">Fecha y hora en que se emite el dictamen.</param>
    /// <param name="aprobado">Indica si el menú fue aprobado.</param>
    /// <param name="observaciones">Señalamientos del revisor, si existen.</param>
    public Validacion(
        Guid nutricionistaId,
        Guid menuId,
        DateTime fechaValidacion,
        bool aprobado,
        string? observaciones = null)
        : base((nutricionistaId, menuId))
    {
        FechaValidacion = fechaValidacion;
        Aprobado = aprobado;
        Observaciones = observaciones;
    }

    /// <summary>FK → Nutricionista revisor (parte de la clave compuesta).</summary>
    public Guid NutricionistaId => Id.NutricionistaId;

    /// <summary>FK → Menú validado (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>Fecha y hora en que se registró la validación.</summary>
    public DateTime FechaValidacion { get; }

    /// <summary>Indica si el dictamen aprobó el menú.</summary>
    public bool Aprobado { get; }

    /// <summary>Observaciones del revisor; puede ser <see langword="null"/>.</summary>
    public string? Observaciones { get; }
}
