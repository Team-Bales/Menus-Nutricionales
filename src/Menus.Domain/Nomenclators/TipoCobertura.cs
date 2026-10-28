namespace Menus.Domain.Nomenclators;

/// <summary>
/// Indica si un grupo nutricional en una dieta es requerido o está restringido (RC-08, RD-16).
/// </summary>
public enum TipoCobertura
{
    /// <summary>El grupo nutricional debe estar presente en el menú.</summary>
    Requerido,

    /// <summary>El grupo nutricional está prohibido en el menú.</summary>
    Restringido,
}
