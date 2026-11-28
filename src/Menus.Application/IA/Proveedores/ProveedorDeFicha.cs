using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Menus.Application.Catalogo.Fichas;
using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.IA.Proveedores;

/// <summary>
/// Fuente <c>ficha</c> (#336, contrato de IA §4): la ficha nutricional de cada ítem del menú valorado, con sus macro y
/// micronutrientes, sus alérgenos y la técnica y el tiempo de preparación.
/// </summary>
/// <remarks>
/// <para>
/// <b>Qué entra.</b> Los ítems del menú valorado que tienen ficha, ordenados por nombre. Los que no tienen se nombran
/// al final del resumen, para que el modelo no les atribuya valores, y se avisa en
/// <see cref="ResultadoDeContexto.Advertencias"/>. De la preparación entran solo la técnica y el tiempo total: los
/// pasos son texto libre largo, que suma tokens y superficie para una inyección en el prompt, y el desglose por
/// ingrediente de un plato repite lo que ya dicen sus alimentos.
/// </para>
/// <para>
/// <b>Privacidad.</b> La ficha es un dato del catálogo, no del paciente, así que no pasa por el
/// <see cref="TransformadorDePaciente"/>. El bloque no lleva identificadores: ni los de los ítems ni los de los
/// ingredientes.
/// </para>
/// <para>
/// <b>Cifras.</b> Los valores se redondean a un decimal y se escriben con coma decimal («31,5 g»). <c>Cifras</c>
/// guarda el mismo valor redondeado, con claves como <c>ficha.1.macronutrientes.proteinasG</c>, para que el
/// <c>VerificadorDeCifras</c> (#327) encuentre cada número del texto (§6). Los números que aparecen dentro de un texto
/// (el nombre de un ítem o de un nutriente, como «B12», o la unidad de la porción, como «ración (350 g)») también van a
/// <c>Cifras</c>.
/// </para>
/// <para>
/// <b>Sin MongoDB.</b> Si el almacén de fichas no está disponible, devuelve un resultado sin bloque y con una
/// advertencia: la tarea sigue con los demás bloques.
/// </para>
/// </remarks>
/// <param name="valoraciones">Repositorio de valoraciones.</param>
/// <param name="menus">Repositorio de menús, para los ítems del menú valorado.</param>
/// <param name="items">Repositorio de alimentos y platos, para su nombre y su clase.</param>
/// <param name="fichas">Almacén de fichas nutricionales (MongoDB).</param>
internal sealed partial class ProveedorDeFicha(
    IValoracionRepository valoraciones,
    IMenuRepository menus,
    IItemAlimentarioRepository items,
    IAlmacenDeFichas fichas) : IProveedorDeContexto
{
    /// <summary>Nombre de la fuente.</summary>
    public const string NombreFuente = "ficha";

    /// <summary>Versión del esquema del bloque.</summary>
    public const int VersionDelBloque = 1;

    /// <summary>Advertencia cuando el almacén de fichas no está disponible.</summary>
    internal const string AvisoSinAlmacen = "La ficha nutricional no está disponible.";

    /// <summary>Advertencia cuando algún ítem del menú no tiene ficha.</summary>
    internal const string AvisoItemsSinFicha = "Hay ítems del menú valorado sin ficha nutricional.";

    /// <summary>Largo máximo de un texto libre de la ficha (técnica, alérgeno, unidad).</summary>
    private const int LargoMaximoDeTexto = 40;

    private static readonly NumberFormatInfo ComaDecimal = new() { NumberDecimalSeparator = "," };

    /// <inheritdoc />
    public string Fuente => NombreFuente;

    /// <inheritdoc />
    public async Task<ResultadoDeContexto?> ConstruirAsync(ContextoDeCaso caso, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(caso);
        if (caso.ValoracionId is not { } valoracionId)
        {
            return null;
        }

        var valoracion = await valoraciones.GetByIdAsync(valoracionId, ct);
        var menu = valoracion is null ? null : await menus.GetByIdAsync(valoracion.MenuId, ct);
        if (menu is null || menu.Items.Count == 0)
        {
            return null;
        }

        var delMenu = (await items.GetByIdsAsync(menu.Items.Select(i => i.ItemId).ToArray(), ct))
            .OrderBy(i => i.Nombre, StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true))
            .ThenBy(i => i.Id)
            .ToList();

        var conFicha = new List<(ItemAlimentario Item, FichaNutricionalDto Ficha)>();
        var sinFicha = new List<string>();
        try
        {
            foreach (var item in delMenu)
            {
                var clase = item is Plato ? ClaseDeItem.Plato : ClaseDeItem.Alimento;
                if (await fichas.ObtenerAsync(clase, item.Id, ct) is { } ficha)
                {
                    conFicha.Add((item, ficha));
                }
                else
                {
                    sinFicha.Add(item.Nombre);
                }
            }
        }
        catch (ServicioNoDisponibleException)
        {
            return new ResultadoDeContexto(null, [AvisoSinAlmacen]);
        }

        if (conFicha.Count == 0)
        {
            return null;
        }

        var cifras = new Dictionary<string, double>(StringComparer.Ordinal);
        var partes = new List<string> { "Ficha nutricional de los ítems del menú valorado." };
        for (var i = 0; i < conFicha.Count; i++)
        {
            partes.Add(DescribirItem(conFicha[i].Item, conFicha[i].Ficha, $"ficha.{i + 1}", cifras));
        }

        var advertencias = new List<string>();
        if (sinFicha.Count > 0)
        {
            var nombres = string.Join(", ", sinFicha);
            NumerosDelTexto(nombres, "ficha.sin-ficha", cifras);
            partes.Add($"Sin ficha registrada: {nombres}.");
            advertencias.Add(AvisoItemsSinFicha);
        }

        var bloque = new BloqueDeContexto(NombreFuente, VersionDelBloque, string.Join(' ', partes), cifras);
        return new ResultadoDeContexto(bloque, advertencias);
    }

    /// <summary>
    /// Una oración por ítem: «Arroz con pollo (plato), por 1 ración (350 g): energía 486 kcal, …; vitaminas: …;
    /// alérgenos: apio; preparación: guiso, 55 min.».
    /// </summary>
    private static string DescribirItem(
        ItemAlimentario item,
        FichaNutricionalDto ficha,
        string clave,
        Dictionary<string, double> cifras)
    {
        NumerosDelTexto(item.Nombre, $"{clave}.nombre", cifras);
        var encabezado = $"{item.Nombre} ({(item is Plato ? "plato" : "alimento")})";

        if (ficha.PorcionReferencia is { } porcion)
        {
            var unidad = TextoLibre(porcion.Unidad);
            NumerosDelTexto(unidad, $"{clave}.porcion.unidad", cifras);
            encabezado += $", por {Numero(porcion.Cantidad, $"{clave}.porcion.cantidad", cifras)} {unidad}";
        }

        var datos = new List<string>();
        datos.AddRange(Nutrientes(ficha.Macronutrientes, null, $"{clave}.macronutrientes", cifras));
        datos.AddRange(Nutrientes(ficha.Micronutrientes, "micronutrientes", $"{clave}.micronutrientes", cifras));
        datos.Add(Alergenos(ficha.Alergenos, $"{clave}.alergenos", cifras));
        if (Preparacion(ficha.ModoPreparacion, $"{clave}.preparacion", cifras) is { } preparacion)
        {
            datos.Add(preparacion);
        }

        return $"{encabezado}: {string.Join("; ", datos)}.";
    }

    /// <summary>
    /// Los valores numéricos de un bloque libre, en grupos: los del primer nivel bajo <paramref name="etiqueta"/>, y
    /// cada subobjeto bajo su propio nombre («vitaminas: A 310 µg, C 22 mg»). Lo que no es un número se omite.
    /// </summary>
    private static IEnumerable<string> Nutrientes(
        JsonObject? bloque,
        string? etiqueta,
        string clave,
        Dictionary<string, double> cifras)
    {
        if (bloque is null)
        {
            yield break;
        }

        var valores = new List<string>();
        var grupos = new List<string>();
        foreach (var (campo, nodo) in bloque)
        {
            // El nombre puede llevar números propios («B12», «omega 3»), que también tienen que estar en las cifras.
            if (nodo is JsonObject subbloque)
            {
                var grupo = NombreDeNutriente.Leer(campo).Nombre;
                NumerosDelTexto(grupo, $"{clave}.{campo}.nombre", cifras);
                grupos.AddRange(Nutrientes(subbloque, grupo, $"{clave}.{campo}", cifras));
            }
            else if (ComoNumero(nodo) is { } valor)
            {
                var (nombre, unidad) = NombreDeNutriente.Leer(campo);
                NumerosDelTexto(nombre, $"{clave}.{campo}.nombre", cifras);
                var texto = $"{nombre} {Numero(valor, $"{clave}.{campo}", cifras)}";
                valores.Add(unidad is null ? texto : $"{texto} {unidad}");
            }
        }

        if (valores.Count > 0)
        {
            var lista = string.Join(", ", valores);
            yield return etiqueta is null ? lista : $"{etiqueta}: {lista}";
        }

        foreach (var grupo in grupos)
        {
            yield return grupo;
        }
    }

    /// <summary>
    /// Los alérgenos, distinguiendo «no registrados» (sin el bloque) de «sin alérgenos» (bloque vacío), como el
    /// documento 1 (§2).
    /// </summary>
    private static string Alergenos(IReadOnlyList<string>? alergenos, string clave, Dictionary<string, double> cifras)
    {
        if (alergenos is null)
        {
            return "alérgenos no registrados";
        }

        var lista = alergenos.Select(TextoLibre).Where(a => a.Length > 0).ToList();
        if (lista.Count == 0)
        {
            return "sin alérgenos";
        }

        var texto = string.Join(", ", lista);
        NumerosDelTexto(texto, clave, cifras);
        return $"alérgenos: {texto}";
    }

    /// <summary>La técnica y el tiempo total de preparación; sin los pasos.</summary>
    private static string? Preparacion(JsonObject? modo, string clave, Dictionary<string, double> cifras)
    {
        if (modo is null)
        {
            return null;
        }

        var datos = new List<string>();
        if (modo["tecnica"] is JsonValue tecnica && tecnica.GetValueKind() == JsonValueKind.String
            && TextoLibre(tecnica.GetValue<string>()) is { Length: > 0 } texto)
        {
            NumerosDelTexto(texto, $"{clave}.tecnica", cifras);
            datos.Add(texto);
        }

        if (ComoNumero(modo["tiempoTotalMin"]) is { } minutos)
        {
            datos.Add($"{Numero(minutos, $"{clave}.tiempoTotalMin", cifras)} min");
        }

        return datos.Count == 0 ? null : $"preparación: {string.Join(", ", datos)}";
    }

    /// <summary>
    /// Redondea a un decimal, guarda el valor en <paramref name="cifras"/> y lo devuelve con coma decimal; sin
    /// decimales si es entero («486», «31,5»).
    /// </summary>
    private static string Numero(double valor, string clave, Dictionary<string, double> cifras)
    {
        var redondeado = Math.Round(valor, 1, MidpointRounding.AwayFromZero);
        cifras[clave] = redondeado;
        return redondeado.ToString("0.#", ComaDecimal);
    }

    /// <summary>
    /// Guarda en <paramref name="cifras"/> los números que aparecen dentro de un texto, leídos como los leerá el
    /// verificador (enteros, o con coma decimal), con claves <c>{clave}.texto.{n}</c>.
    /// </summary>
    private static void NumerosDelTexto(string texto, string clave, Dictionary<string, double> cifras)
    {
        var n = 0;
        foreach (Match numero in NumeroEnTexto().Matches(texto))
        {
            cifras[$"{clave}.texto.{++n}"] = double.Parse(numero.Value.Replace(',', '.'), CultureInfo.InvariantCulture);
        }
    }

    /// <summary>El valor de un nodo si es un número JSON; si no, <see langword="null"/>.</summary>
    private static double? ComoNumero(JsonNode? nodo)
        => nodo is JsonValue valor && valor.GetValueKind() == JsonValueKind.Number
            ? double.Parse(valor.ToJsonString(), CultureInfo.InvariantCulture)
            : null;

    /// <summary>
    /// Un texto libre de la ficha en una sola línea, sin espacios de más y con <see cref="LargoMaximoDeTexto"/>
    /// caracteres como máximo.
    /// </summary>
    private static string TextoLibre(string texto)
    {
        var limpio = Espacios().Replace(texto, " ").Trim();
        return limpio.Length <= LargoMaximoDeTexto ? limpio : string.Concat(limpio.AsSpan(0, LargoMaximoDeTexto).TrimEnd(), "…");
    }

    [GeneratedRegex(@"\d+(?:,\d+)?")]
    private static partial Regex NumeroEnTexto();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();
}
