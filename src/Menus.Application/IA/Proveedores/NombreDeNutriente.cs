using System.Text.RegularExpressions;

namespace Menus.Application.IA.Proveedores;

/// <summary>
/// Convierte el nombre de un campo de la ficha nutricional en texto legible: separa las palabras y lee la unidad del
/// sufijo (<c>proteinasG</c> → «proteínas», «g»; <c>b12Ug</c> → «B12», «µg»).
/// </summary>
/// <remarks>
/// Los nombres siguen la convención del documento 1 de <c>docs/Ficha nutricional MongoDB</c> (§2): camelCase y la
/// unidad como sufijo. Los bloques no tienen campos fijos (RM-05), así que no hay una tabla de nutrientes: se lee el
/// nombre tal como venga. Un sufijo que no es una unidad conocida queda como una palabra más, y el valor sale sin
/// unidad.
/// </remarks>
internal static partial class NombreDeNutriente
{
    private static readonly Dictionary<string, string> Unidades = new(StringComparer.Ordinal)
    {
        ["Kcal"] = "kcal",
        ["Kj"] = "kJ",
        ["G"] = "g",
        ["Mg"] = "mg",
        ["Ug"] = "µg",
        ["Mcg"] = "µg",
        ["Ml"] = "ml",
        ["Min"] = "min",
    };

    // Solo las palabras habituales de una ficha que llevan tilde; el resto se deja como está.
    private static readonly Dictionary<string, string> ConTilde = new(StringComparer.Ordinal)
    {
        ["energia"] = "energía",
        ["proteinas"] = "proteínas",
        ["azucares"] = "azúcares",
        ["lipidos"] = "lípidos",
        ["calorias"] = "calorías",
        ["fosforo"] = "fósforo",
        ["acido"] = "ácido",
        ["folico"] = "fólico",
        ["indice"] = "índice",
        ["glucemico"] = "glucémico",
        ["tecnica"] = "técnica",
    };

    /// <summary>El nombre legible y la unidad de un campo.</summary>
    /// <param name="campo">Nombre del campo en la ficha, por ejemplo <c>grasasSaturadasG</c>.</param>
    /// <returns>El nombre («grasas saturadas») y la unidad («g»), o <see langword="null"/> si no tiene.</returns>
    public static (string Nombre, string? Unidad) Leer(string campo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campo);
        var palabras = InicioDePalabra().Split(campo).Where(p => p.Length > 0).ToList();

        string? unidad = null;
        if (palabras.Count > 1 && Unidades.TryGetValue(palabras[^1], out var leida))
        {
            unidad = leida;
            palabras.RemoveAt(palabras.Count - 1);
        }

        var nombre = string.Join(' ', palabras.Select(Palabra));
        return (nombre, unidad);
    }

    private static string Palabra(string palabra)
    {
        var minuscula = palabra.ToLowerInvariant();

        // Una letra sola, o una letra con números, es una vitamina: «A», «B12».
        if (Vitamina().IsMatch(minuscula))
        {
            return minuscula.ToUpperInvariant();
        }

        return ConTilde.GetValueOrDefault(minuscula, minuscula);
    }

    [GeneratedRegex("(?<!^)(?=[A-Z])")]
    private static partial Regex InicioDePalabra();

    [GeneratedRegex(@"^[a-z]\d*$")]
    private static partial Regex Vitamina();
}
