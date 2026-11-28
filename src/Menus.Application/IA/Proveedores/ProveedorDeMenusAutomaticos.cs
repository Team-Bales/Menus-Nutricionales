using System.Globalization;
using Menus.Application.Reportes;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.IA.Proveedores;

/// <summary>
/// Fuente <c>reporte-1</c> (#335): los menús generados automáticamente en la dieta del menú valorado (consulta 1).
/// </summary>
/// <remarks>
/// <para>
/// Dice cuántos hay, si el menú valorado es uno de ellos y, de los 5 más recientes, la fecha relativa, la cantidad de
/// alimentos, la distribución por tipo de preparación y los grupos cubiertos.
/// </para>
/// <para>
/// <b>Privacidad:</b> la dieta no se nombra, porque ya la dice (o la generaliza) el bloque <c>valoracion</c>. El
/// nombre del creador no entra, porque es un dato personal que no le sirve al modelo.
/// </para>
/// </remarks>
/// <param name="valoraciones">Repositorio de valoraciones.</param>
/// <param name="menus">Repositorio de menús.</param>
/// <param name="consultas">Consultas informacionales (#90).</param>
internal sealed class ProveedorDeMenusAutomaticos(
    IValoracionRepository valoraciones,
    IMenuRepository menus,
    IConsultasInformacionales consultas) : IProveedorDeContexto
{
    /// <summary>Nombre de la fuente.</summary>
    public const string NombreFuente = "reporte-1";

    private const int MaximoDeMenus = 5;

    /// <inheritdoc />
    public string Fuente => NombreFuente;

    /// <inheritdoc />
    public async Task<ResultadoDeContexto?> ConstruirAsync(ContextoDeCaso caso, CancellationToken ct = default)
    {
        if (await ReporteDelCaso.CargarAsync(caso, valoraciones, menus, ct) is not { } reporte)
        {
            return null;
        }

        var filas = await consultas.ConsultarMenusAutomaticosAsync(reporte.Menu.DietaId, ct);
        if (filas.Count == 0)
        {
            return null;
        }

        var cifras = new Dictionary<string, double>(StringComparer.Ordinal);
        var partes = new List<string>
        {
            $"Menús generados automáticamente en la dieta del menú: {TextoDeBloque.Cantidad(filas.Count, "menú", "menús", "reporte-1.total", cifras)}. "
            + (reporte.Menu.EsAutomatico ? "El menú valorado es uno de ellos." : "El menú valorado no se generó automáticamente."),
        };

        var recientes = filas.OrderByDescending(f => f.FechaCreacion).ThenBy(f => f.MenuId).Take(MaximoDeMenus).ToList();
        var descripciones = recientes.Select((fila, i) => Describir(fila, i + 1, reporte, caso.FechaReferencia, cifras));
        var titulo = filas.Count > MaximoDeMenus ? $"Los {MaximoDeMenus} más recientes" : "Por fecha";
        if (filas.Count > MaximoDeMenus)
        {
            cifras["reporte-1.mostrados"] = MaximoDeMenus;
        }

        partes.Add($"{titulo}: {string.Join("; ", descripciones)}.");
        return ReporteDelCaso.Resultado(NombreFuente, partes, cifras);
    }

    private static string Describir(MenuAutomaticoDto fila, int n, ReporteDelCaso reporte, DateOnly referencia, Dictionary<string, double> cifras)
    {
        var clave = $"reporte-1.{n}";
        var fecha = TextoDeBloque.FechaRelativa(DateOnly.FromDateTime(fila.FechaCreacion.UtcDateTime), referencia, $"{clave}.dias", cifras);
        var texto = $"creado {fecha}{(fila.MenuId == reporte.Menu.Id ? " (el menú valorado)" : string.Empty)}, "
            + TextoDeBloque.Cantidad(fila.CantidadTotalAlimentos, "alimento", "alimentos", $"{clave}.alimentos", cifras);

        if (Distribucion(fila.Distribucion, $"{clave}.distribucion", cifras) is { } distribucion)
        {
            texto += $", distribución {distribucion}";
        }

        if (!string.IsNullOrWhiteSpace(fila.GruposCubiertos))
        {
            var grupos = TextoDeBloque.TextoLibre(fila.GruposCubiertos, 200);
            TextoDeBloque.NumerosDelTexto(grupos, $"{clave}.grupos", cifras);
            texto += $", grupos cubiertos: {grupos}";
        }

        return texto;
    }

    /// <summary>
    /// La distribución como porcentajes («Plato fuerte 50 %, Postre 25 %»). La consulta la devuelve como
    /// «Plato fuerte: 0.50, Postre: 0.25», con proporciones entre 0 y 1 y punto decimal.
    /// </summary>
    /// <returns>El texto, o <see langword="null"/> si no hay distribución o no tiene esa forma.</returns>
    private static string? Distribucion(string texto, string clave, Dictionary<string, double> cifras)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var tipos = new List<(string Tipo, int Porcentaje)>();
        foreach (var entrada in texto.Split(", "))
        {
            var separador = entrada.LastIndexOf(": ", StringComparison.Ordinal);
            if (separador <= 0
                || !decimal.TryParse(entrada[(separador + 2)..], NumberStyles.Number, CultureInfo.InvariantCulture, out var proporcion))
            {
                return null;
            }

            tipos.Add((entrada[..separador].Trim(), (int)Math.Round(proporcion * 100, MidpointRounding.AwayFromZero)));
        }

        return string.Join(", ", tipos.Select((t, i) =>
        {
            TextoDeBloque.NumerosDelTexto(t.Tipo, $"{clave}.{i + 1}.tipo", cifras);
            cifras[$"{clave}.{i + 1}.porcentaje"] = t.Porcentaje;
            return string.Create(CultureInfo.InvariantCulture, $"{t.Tipo} {t.Porcentaje} %");
        }));
    }
}
