using System.Globalization;
using System.Text.RegularExpressions;

namespace Menus.Application.IA.Proveedores;

/// <summary>
/// Formatos que comparten los proveedores de contexto (#325, #335): los números que aparecen en el <c>Resumen</c> y
/// los que se guardan en <c>Cifras</c> tienen que ser los mismos, para que el <c>VerificadorDeCifras</c> (#327) los
/// encuentre (contrato de IA, §6).
/// </summary>
/// <remarks>
/// Los decimales se escriben con coma, como en el #336: así los escribe el modelo en español y así los lee el
/// nutricionista. El verificador tiene que leer la coma como separador decimal (contrato, §6).
/// </remarks>
internal static partial class TextoDeBloque
{
    private static readonly NumberFormatInfo ComaDecimal = new() { NumberDecimalSeparator = ",", NegativeSign = "-" };

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
    /// Agrega a <paramref name="cifras"/> las tres cifras de <see cref="PorcentajeConConteos(int, int)"/>, con las claves
    /// <c>{clave}.porcentaje</c>, <c>{clave}.parte</c> y <c>{clave}.total</c>, y devuelve el mismo texto.
    /// </summary>
    /// <param name="parte">Cantidad que se mide.</param>
    /// <param name="total">Total, mayor que cero.</param>
    /// <param name="clave">Prefijo de las claves.</param>
    /// <param name="cifras">Cifras del bloque.</param>
    /// <returns>El texto con el porcentaje y los conteos.</returns>
    public static string PorcentajeConConteos(int parte, int total, string clave, IDictionary<string, double> cifras)
    {
        cifras[$"{clave}.porcentaje"] = Porcentaje(parte, total);
        cifras[$"{clave}.parte"] = parte;
        cifras[$"{clave}.total"] = total;
        return PorcentajeConConteos(parte, total);
    }

    /// <summary>
    /// «del paciente» o «de P1»: la referencia opaca del paciente con su preposición (contrato de IA, §5.1).
    /// </summary>
    /// <param name="referencia">«el paciente», o un código opaco como «P1».</param>
    /// <returns>La referencia precedida de «de», con la contracción cuando corresponde.</returns>
    public static string DeLaReferencia(string referencia)
        => referencia.StartsWith("el ", StringComparison.Ordinal) ? $"del {referencia[3..]}" : $"de {referencia}";

    /// <summary>
    /// Redondea <paramref name="valor"/>, lo guarda en <paramref name="cifras"/> y lo devuelve con coma decimal y sin
    /// ceros de más («0,45», «3,4», «12»).
    /// </summary>
    /// <param name="valor">El valor.</param>
    /// <param name="decimales">Decimales como máximo.</param>
    /// <param name="clave">Clave de la cifra.</param>
    /// <param name="cifras">Cifras del bloque.</param>
    /// <returns>El valor redondeado, como texto.</returns>
    public static string Numero(double valor, int decimales, string clave, IDictionary<string, double> cifras)
    {
        var redondeado = Math.Round(valor, decimales, MidpointRounding.AwayFromZero);
        if (redondeado == 0)
        {
            // Sin «-0».
            redondeado = 0;
        }

        cifras[clave] = redondeado;
        return redondeado.ToString("0." + new string('#', decimales), ComaDecimal);
    }

    /// <summary>
    /// Una cantidad entera con su sustantivo en singular o en plural («1 ítem», «12 ítems»), guardada en
    /// <paramref name="cifras"/>.
    /// </summary>
    /// <param name="cantidad">La cantidad.</param>
    /// <param name="singular">El sustantivo en singular.</param>
    /// <param name="plural">El sustantivo en plural.</param>
    /// <param name="clave">Clave de la cifra.</param>
    /// <param name="cifras">Cifras del bloque.</param>
    /// <returns>La cantidad y el sustantivo.</returns>
    public static string Cantidad(long cantidad, string singular, string plural, string clave, IDictionary<string, double> cifras)
    {
        cifras[clave] = cantidad;
        return string.Create(CultureInfo.InvariantCulture, $"{cantidad} {(cantidad == 1 ? singular : plural)}");
    }

    /// <summary>
    /// La fecha como relativa a la del caso («hoy», «hace 1 día», «hace 12 días», «dentro de 3 días»), como pide el
    /// §5.1 para las fechas exactas. Guarda los días en <paramref name="cifras"/>.
    /// </summary>
    /// <remarks>
    /// Es la misma regla que aplica el <see cref="TransformadorDePaciente"/> a las fechas del paciente, con el
    /// singular para un día.
    /// </remarks>
    /// <param name="fecha">La fecha.</param>
    /// <param name="referencia">La fecha del caso (<see cref="ContextoDeCaso.FechaReferencia"/>).</param>
    /// <param name="clave">Clave de la cifra.</param>
    /// <param name="cifras">Cifras del bloque.</param>
    /// <returns>La fecha relativa.</returns>
    public static string FechaRelativa(DateOnly fecha, DateOnly referencia, string clave, IDictionary<string, double> cifras)
    {
        var dias = fecha.DayNumber - referencia.DayNumber;
        if (dias == 0)
        {
            return "hoy";
        }

        cifras[clave] = Math.Abs(dias);
        var cuantos = Math.Abs(dias) == 1 ? "1 día" : string.Create(CultureInfo.InvariantCulture, $"{Math.Abs(dias)} días");
        return dias < 0 ? $"hace {cuantos}" : $"dentro de {cuantos}";
    }

    /// <summary>
    /// Guarda en <paramref name="cifras"/> los números que aparecen dentro de un texto (un nombre, una observación),
    /// leídos como los leerá el verificador (enteros, o con coma decimal), con claves <c>{clave}.texto.{n}</c>.
    /// </summary>
    /// <param name="texto">El texto.</param>
    /// <param name="clave">Prefijo de las claves.</param>
    /// <param name="cifras">Cifras del bloque.</param>
    public static void NumerosDelTexto(string texto, string clave, IDictionary<string, double> cifras)
    {
        var n = 0;
        foreach (Match numero in NumeroEnTexto().Matches(texto))
        {
            cifras[$"{clave}.texto.{++n}"] = double.Parse(numero.Value.Replace(',', '.'), CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Un texto libre en una sola línea, sin espacios de más y con <paramref name="largo"/> caracteres como máximo.
    /// </summary>
    /// <param name="texto">El texto.</param>
    /// <param name="largo">Largo máximo.</param>
    /// <returns>El texto limpio, terminado en «…» si se recortó.</returns>
    public static string TextoLibre(string texto, int largo)
    {
        var limpio = Espacios().Replace(texto, " ").Trim();
        return limpio.Length <= largo ? limpio : string.Concat(limpio.AsSpan(0, largo).TrimEnd(), "…");
    }

    [GeneratedRegex(@"\d+(?:,\d+)?")]
    private static partial Regex NumeroEnTexto();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();
}
