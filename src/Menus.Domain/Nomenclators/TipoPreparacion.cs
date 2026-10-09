namespace Menus.Domain.Nomenclators;

/// <summary>
/// Clasificación del tipo de preparación de un alimento o plato dentro de un menú.
/// Determina en qué parte de la comida se sirve y se usa como criterio de
/// distribución durante la generación automática de menús.
/// </summary>
public enum TipoPreparacion
{
    /// <summary>Plato servido al inicio de la comida (aperitivo, sopa, ensalada).</summary>
    Entrante,

    /// <summary>Plato principal de la comida, de mayor aporte calórico.</summary>
    PlatoFuerte,

    /// <summary>Plato dulce o fruta servido al final de la comida.</summary>
    Postre,

    /// <summary>Líquido que acompaña la comida (agua, jugo, infusión).</summary>
    Bebida,
}
