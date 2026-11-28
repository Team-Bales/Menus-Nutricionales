namespace Menus.Application.IA.Proveedores;

/// <summary>
/// Límites de los rangos de edad que produce el <see cref="TransformadorDePaciente"/> (contrato de IA, §5.1):
/// 0-12, 13-17, 18-39, 40-64 y 65+.
/// </summary>
/// <remarks>
/// El transformador pasa el rango como texto a la función de conteo. Para contar en la base hacen falta los
/// números, así que se leen del texto en lugar de repetir aquí la tabla de rangos.
/// </remarks>
internal static class RangoDeEdad
{
    /// <summary>Convierte «18-39» en (18, 39) y «65+» en (65, sin tope).</summary>
    /// <param name="rango">El rango, tal como lo produce el transformador.</param>
    /// <returns>La edad mínima y la máxima, ambas incluidas; la máxima es <see langword="null"/> si no hay tope.</returns>
    /// <exception cref="FormatException">Si el texto no es un rango con ese formato.</exception>
    public static (int Minima, int? Maxima) Limites(string rango)
    {
        if (rango.EndsWith('+') && int.TryParse(rango[..^1], out var desde))
        {
            return (desde, null);
        }

        var partes = rango.Split('-');
        if (partes.Length == 2 && int.TryParse(partes[0], out var minima) && int.TryParse(partes[1], out var maxima))
        {
            return (minima, maxima);
        }

        throw new FormatException($"«{rango}» no es un rango de edad del transformador.");
    }
}
