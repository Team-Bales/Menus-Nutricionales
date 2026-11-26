using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;

namespace Menus.Application.Catalogo.Fichas;

/// <summary>Reglas que comparten los casos de uso y el validador de la ficha nutricional (#283).</summary>
internal static class ReglasDeLaFicha
{
    /// <summary>Valor de <c>origen.tabla</c> de un ítem de la clase dada (RM-02).</summary>
    /// <param name="clase">Clase del ítem.</param>
    /// <returns><c>Alimento</c> o <c>Plato</c>.</returns>
    public static string Tabla(ClaseDeItem clase) => clase == ClaseDeItem.Plato ? "Plato" : "Alimento";

    /// <summary>Valor de <c>tipo</c> que corresponde a la clase (RM-03).</summary>
    /// <param name="clase">Clase del ítem.</param>
    /// <returns><c>simple</c> para un alimento y <c>compuesto</c> para un plato.</returns>
    public static string Tipo(ClaseDeItem clase) => clase == ClaseDeItem.Plato ? "compuesto" : "simple";

    /// <summary>Nombre del recurso en los mensajes («alimento» o «plato»).</summary>
    /// <param name="clase">Clase del ítem.</param>
    /// <returns>El nombre en minúsculas.</returns>
    public static string Recurso(ClaseDeItem clase) => clase == ClaseDeItem.Plato ? "plato" : "alimento";

    /// <summary>
    /// Forma de un alérgeno para compararlo: sin espacios a los lados, en minúsculas y sin acentos (ADR-009, (b)). La
    /// ficha guarda el texto como se escribió; esto solo sirve para detectar repetidos (RM-04).
    /// </summary>
    /// <param name="alergeno">El alérgeno escrito.</param>
    /// <returns>El texto normalizado.</returns>
    public static string NormalizarAlergeno(string alergeno)
    {
        var descompuesto = alergeno.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sinAcentos = new StringBuilder(descompuesto.Length);
        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                sinAcentos.Append(caracter);
            }
        }

        return sinAcentos.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Indica si todos los nombres de campo de un bloque, a cualquier profundidad, se pueden guardar en MongoDB: no
    /// vacíos, sin «$» al principio y sin «.».
    /// </summary>
    /// <remarks>
    /// MongoDB reserva el «$» para sus operadores y usa el «.» para recorrer subdocumentos, así que un campo así no se
    /// podría consultar ni actualizar sin ambigüedad. Los bloques no tienen campos fijos (RM-05), pero sus nombres sí
    /// tienen que ser nombres.
    /// </remarks>
    /// <param name="nodo">El bloque, o un valor dentro de él.</param>
    /// <returns><see langword="true"/> si no hay ningún nombre inválido.</returns>
    public static bool NombresDeCampoValidos(JsonNode? nodo) => nodo switch
    {
        JsonObject objeto => objeto.All(campo => NombreValido(campo.Key) && NombresDeCampoValidos(campo.Value)),
        JsonArray arreglo => arreglo.All(NombresDeCampoValidos),
        _ => true,
    };

    /// <summary>
    /// Exige que quien escribe la ficha sea el nutricionista que describió el ítem: RS-02, ampliada a la ficha como el
    /// ADR-012 la amplió a la composición del plato.
    /// </summary>
    /// <param name="item">El alimento o el plato.</param>
    /// <param name="nutricionistaId">Actor de la petición.</param>
    /// <exception cref="ReglaDeNegocioException">Si el actor no es quien describió el ítem.</exception>
    public static void ExigirDescriptor(ItemAlimentario item, Guid nutricionistaId)
    {
        if (item.NutricionistaId != nutricionistaId)
        {
            throw new ReglaDeNegocioException(
                "Solo el nutricionista que describió el ítem puede escribir su ficha nutricional (RS-02).",
                "RS-02");
        }
    }

    private static bool NombreValido(string nombre)
        => !string.IsNullOrWhiteSpace(nombre) && !nombre.StartsWith('$') && !nombre.Contains('.');
}
