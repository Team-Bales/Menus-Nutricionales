using System.Globalization;

namespace Menus.Application.IA.Proveedores;

/// <summary>
/// Formatos que comparten los proveedores de contexto del #325: los números que aparecen en el <c>Resumen</c> y los
/// que se guardan en <c>Cifras</c> tienen que ser los mismos, para que el <c>VerificadorDeCifras</c> (#327) los
/// encuentre (contrato de IA, §6).
/// </summary>
internal static class TextoDeBloque
{
    /// <summary>Porcentaje entero, redondeado hacia arriba en el empate (72,5 → 73).</summary>
    /// <param name="parte">Cantidad que se mide (por ejemplo, ítems aceptados).</param>
    /// <param name="total">Total, mayor que cero.</param>
    /// <returns>El porcentaje entre 0 y 100.</returns>
    public static int Porcentaje(int parte, int total)
        => (int)Math.Round(parte * 100m / total, MidpointRounding.AwayFromZero);

    /// <summary>«72 % (18 de 25)»: el porcentaje y los conteos que lo explican.</summary>
    /// <param name="parte">Cantidad que se mide.</param>
    /// <param name="total">Total, mayor que cero.</param>
    /// <returns>El texto con el porcentaje y los conteos.</returns>
    public static string PorcentajeConConteos(int parte, int total)
        => string.Create(CultureInfo.InvariantCulture, $"{Porcentaje(parte, total)} % ({parte} de {total})");

    /// <summary>
    /// «del paciente» o «de P1»: la referencia opaca del paciente con su preposición (contrato de IA, §5.1).
    /// </summary>
    /// <param name="referencia">«el paciente», o un código opaco como «P1».</param>
    /// <returns>La referencia precedida de «de», con la contracción cuando corresponde.</returns>
    public static string DeLaReferencia(string referencia)
        => referencia.StartsWith("el ", StringComparison.Ordinal) ? $"del {referencia[3..]}" : $"de {referencia}";
}
