namespace Menus.Domain.Nomenclators;

/// <summary>
/// Indica si un grupo nutricional en una dieta es requerido o está restringido (R6-05b, R6-17b).
/// </summary>
public enum TipoCobertura
{
    /// <summary>El grupo nutricional debe estar presente en el menú.</summary>
    Requerido,

    /// <summary>El grupo nutricional está prohibido en el menú.</summary>
    Restringido,
}
