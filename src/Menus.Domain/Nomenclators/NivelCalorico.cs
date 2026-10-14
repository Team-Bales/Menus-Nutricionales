namespace Menus.Domain.Nomenclators;

/// <summary>
/// Clasificación del aporte calórico de un alimento o plato.
/// El nutricionista asigna este nivel al registrar el alimento en el sistema.
/// Se utiliza como criterio de equilibrio durante la generación automática de menús
/// y como dimensión de análisis en los reportes de desempeño nutricional.
/// </summary>
public enum NivelCalorico
{
    /// <summary>Aporte calórico reducido (menos de 100 kcal por porción estándar).</summary>
    Bajo,

    /// <summary>Aporte calórico moderado (entre 100 y 300 kcal por porción estándar).</summary>
    Medio,

    /// <summary>Aporte calórico elevado (más de 300 kcal por porción estándar).</summary>
    Alto,
}
