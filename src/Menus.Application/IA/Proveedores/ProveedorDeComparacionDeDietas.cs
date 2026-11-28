using Menus.Application.Reportes;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;

namespace Menus.Application.IA.Proveedores;

/// <summary>
/// Fuente <c>reporte-5</c> (#335): la composición de los menús finales de la dieta del menú valorado y los criterios
/// de equilibrio del menú valorado (consulta 5).
/// </summary>
/// <remarks>
/// <para>
/// La consulta compara dos o más dietas, pero el caso tiene una sola, la del menú (decisión del #335). Por eso se
/// consulta solo esa dieta: su distribución de ítems por grupo nutricional (los 5 grupos con más ítems) y por nivel
/// calórico, y la fila de criterios del menú valorado. El puerto acepta una lista de una dieta; el mínimo de dos lo
/// exige el validador de la API, que aquí no interviene.
/// </para>
/// <para>
/// Se cuentan solo los menús finales (aprobados), que son los que se sirven. La dieta no se nombra (ver
/// <see cref="ProveedorDeMenusAutomaticos"/>).
/// </para>
/// </remarks>
/// <param name="valoraciones">Repositorio de valoraciones.</param>
/// <param name="menus">Repositorio de menús.</param>
/// <param name="consultas">Consultas informacionales (#90).</param>
internal sealed class ProveedorDeComparacionDeDietas(
    IValoracionRepository valoraciones,
    IMenuRepository menus,
    IConsultasInformacionales consultas) : IProveedorDeContexto
{
    /// <summary>Nombre de la fuente.</summary>
    public const string NombreFuente = "reporte-5";

    private const int MaximoDeGrupos = 5;

    /// <inheritdoc />
    public string Fuente => NombreFuente;

    /// <inheritdoc />
    public async Task<ResultadoDeContexto?> ConstruirAsync(ContextoDeCaso caso, CancellationToken ct = default)
    {
        if (await ReporteDelCaso.CargarAsync(caso, valoraciones, menus, ct) is not { } reporte)
        {
            return null;
        }

        Guid[] dieta = [reporte.Menu.DietaId];
        var porGrupo = await consultas.ConsultarComparacionPorGrupoAsync(dieta, soloFinales: true, ct);
        var porNivel = await consultas.ConsultarComparacionPorNivelAsync(dieta, soloFinales: true, ct);
        var criterios = (await consultas.ConsultarCriteriosEquilibrioAsync(dieta, soloFinales: true, ct))
            .FirstOrDefault(c => c.MenuId == reporte.Menu.Id);

        var cifras = new Dictionary<string, double>(StringComparer.Ordinal);
        var partes = new List<string>();

        if (porGrupo.Count > 0 || porNivel.Count > 0)
        {
            partes.Add("Composición de los menús finales de la dieta del menú.");
        }

        if (porGrupo.Count > 0)
        {
            var grupos = porGrupo
                .OrderByDescending(g => g.Items)
                .ThenBy(g => g.GrupoNutricional, StringComparer.Ordinal)
                .Take(MaximoDeGrupos)
                .Select((g, i) => Proporcion(g.GrupoNutricional, g.Porcentaje, g.Items, $"reporte-5.grupo.{i + 1}", cifras));
            var texto = $"Por grupo nutricional: {string.Join("; ", grupos)}";
            if (porGrupo.Count > MaximoDeGrupos)
            {
                texto += $"; y {TextoDeBloque.Cantidad(porGrupo.Count - MaximoDeGrupos, "grupo más", "grupos más", "reporte-5.grupo.restantes", cifras)}";
            }

            partes.Add(texto + ".");
        }

        if (porNivel.Count > 0)
        {
            var niveles = porNivel
                .OrderBy(n => OrdenDelNivel(n.NivelCalorico))
                .ThenBy(n => n.NivelCalorico, StringComparer.Ordinal)
                .Select(n => Proporcion(n.NivelCalorico, n.Porcentaje, n.Items, $"reporte-5.nivel.{n.NivelCalorico}", cifras));
            partes.Add($"Por nivel calórico: {string.Join("; ", niveles)}.");
        }

        if (criterios is not null)
        {
            partes.Add(Criterios(criterios, cifras));
        }

        return ReporteDelCaso.Resultado(NombreFuente, partes, cifras);
    }

    /// <summary>«Cereales 30 % (12 ítems)».</summary>
    private static string Proporcion(string nombre, decimal porcentaje, long items, string clave, Dictionary<string, double> cifras)
    {
        TextoDeBloque.NumerosDelTexto(nombre, $"{clave}.nombre", cifras);
        var entero = (int)Math.Round(porcentaje, MidpointRounding.AwayFromZero);
        cifras[$"{clave}.porcentaje"] = entero;
        return $"{nombre} {entero} % ({TextoDeBloque.Cantidad(items, "ítem", "ítems", $"{clave}.items", cifras)})";
    }

    /// <summary>Los cuatro criterios de equilibrio del menú valorado, con lo que falla si algo falla.</summary>
    private static string Criterios(CriteriosEquilibrioDto c, Dictionary<string, double> cifras)
    {
        var cantidad = $"{TextoDeBloque.Cantidad(c.Items, "ítem", "ítems", "reporte-5.criterios.items", cifras)} de "
            + $"{TextoDeBloque.Cantidad(c.CantidadDeclarada, "declarado", "declarados", "reporte-5.criterios.declarados", cifras)}"
            + (c.CantidadCumple ? " (cumple)" : " (no cumple)");
        var proporciones = c.ProporcionesCumplen
            ? "cumplen"
            : Con("no cumplen", "fuera de proporción", c.TiposFueraDeProporcion, "reporte-5.criterios.fueraDeProporcion", cifras);
        var requeridos = c.RequeridosCubiertos
            ? "cubiertos"
            : Con("no cubiertos", "faltan", c.RequeridosFaltantes, "reporte-5.criterios.faltantes", cifras);
        var prohibidos = c.ProhibidosAusentes
            ? "ausentes"
            : Con("presentes", "presentes", c.ProhibidosPresentes, "reporte-5.criterios.prohibidos", cifras);

        return $"Criterios de equilibrio del menú valorado: {cantidad}; proporciones por tipo de preparación: {proporciones}; "
            + $"grupos requeridos: {requeridos}; grupos prohibidos: {prohibidos}.";
    }

    /// <summary>«no cumplen (fuera de proporción: Postre)», o solo el estado si la consulta no trae la lista.</summary>
    private static string Con(string estado, string etiqueta, string? lista, string clave, Dictionary<string, double> cifras)
    {
        if (string.IsNullOrWhiteSpace(lista))
        {
            return estado;
        }

        var texto = TextoDeBloque.TextoLibre(lista, 200);
        TextoDeBloque.NumerosDelTexto(texto, clave, cifras);
        return $"{estado} ({etiqueta}: {texto})";
    }

    private static int OrdenDelNivel(string nivel)
    {
        var indice = NivelCalorico.Todos.ToList().FindIndex(n => n.Nombre == nivel);
        return indice < 0 ? int.MaxValue : indice;
    }
}
