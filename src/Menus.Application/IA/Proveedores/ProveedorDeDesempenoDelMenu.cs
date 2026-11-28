using Menus.Application.Reportes;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;

namespace Menus.Application.IA.Proveedores;

/// <summary>
/// Fuente <c>reporte-4</c> (#335): el desempeño de todos los pacientes que recibieron el menú valorado (consulta 4).
/// </summary>
/// <remarks>
/// <para>
/// Da la aceptación global y por nivel calórico, los 5 ítems con menor aceptación (entre los que tuvieron algún
/// rechazo) y los ítems sin consumos registrados.
/// </para>
/// <para>
/// Es el complemento de <c>aceptacion</c> (#325): aquella cuenta solo al paciente del caso, y esta a todos los
/// pacientes del menú. Son agregados, sin datos de ningún paciente.
/// </para>
/// </remarks>
/// <param name="valoraciones">Repositorio de valoraciones.</param>
/// <param name="menus">Repositorio de menús.</param>
/// <param name="consultas">Consultas informacionales (#90).</param>
internal sealed class ProveedorDeDesempenoDelMenu(
    IValoracionRepository valoraciones,
    IMenuRepository menus,
    IConsultasInformacionales consultas) : IProveedorDeContexto
{
    /// <summary>Nombre de la fuente.</summary>
    public const string NombreFuente = "reporte-4";

    private const int MaximoDeItems = 5;

    /// <inheritdoc />
    public string Fuente => NombreFuente;

    /// <inheritdoc />
    public async Task<ResultadoDeContexto?> ConstruirAsync(ContextoDeCaso caso, CancellationToken ct = default)
    {
        if (await ReporteDelCaso.CargarAsync(caso, valoraciones, menus, ct) is not { } reporte)
        {
            return null;
        }

        var filas = await consultas.ConsultarDesempenoMenuAsync(reporte.Menu.Id, ct);
        var conConsumos = filas.Where(f => f.ServiciosConConsumo > 0).ToList();
        if (conConsumos.Count == 0)
        {
            return null;
        }

        var cifras = new Dictionary<string, double>(StringComparer.Ordinal);
        var partes = new List<string>
        {
            "Desempeño de todos los pacientes que recibieron el menú valorado: aceptaron el "
            + $"{Tasa(conConsumos, "reporte-4.global", cifras)} de los ítems servidos.",
        };

        var niveles = conConsumos
            .GroupBy(f => f.NivelCalorico)
            .OrderBy(g => OrdenDelNivel(g.Key))
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g =>
            {
                TextoDeBloque.NumerosDelTexto(g.Key, $"reporte-4.nivel.{g.Key}.nombre", cifras);
                return $"{g.Key} {Tasa(g, $"reporte-4.nivel.{g.Key}", cifras)}";
            });
        partes.Add($"Por nivel calórico: {string.Join("; ", niveles)}.");

        var menosAceptados = conConsumos
            .Where(f => f.Rechazos > 0)
            .OrderBy(f => (double)f.Aceptaciones / f.ServiciosConConsumo)
            .ThenByDescending(f => f.ServiciosConConsumo)
            .ThenBy(f => f.Item, StringComparer.Ordinal)
            .Take(MaximoDeItems)
            .Select((f, i) =>
            {
                TextoDeBloque.NumerosDelTexto(f.Item, $"reporte-4.menosAceptado.{i + 1}.nombre", cifras);
                return $"{f.Item} (nivel {f.NivelCalorico}), {Tasa([f], $"reporte-4.menosAceptado.{i + 1}", cifras)}";
            })
            .ToList();
        if (menosAceptados.Count > 0)
        {
            partes.Add($"Ítems con menor aceptación: {string.Join("; ", menosAceptados)}.");
        }

        var sinConsumos = filas.Where(f => f.ServiciosConConsumo == 0).Select(f => f.Item).ToList();
        if (sinConsumos.Count > 0)
        {
            var nombres = string.Join(", ", sinConsumos);
            TextoDeBloque.NumerosDelTexto(nombres, "reporte-4.sinConsumos", cifras);
            partes.Add($"Sin consumos registrados: {nombres}.");
        }

        return ReporteDelCaso.Resultado(NombreFuente, partes, cifras);
    }

    /// <summary>«68 % (34 de 50)»: aceptaciones sobre servicios con consumo.</summary>
    private static string Tasa(IEnumerable<DesempenoItemDto> filas, string clave, Dictionary<string, double> cifras)
    {
        var lista = filas.ToList();
        return TextoDeBloque.PorcentajeConConteos(
            (int)lista.Sum(f => f.Aceptaciones), (int)lista.Sum(f => f.ServiciosConConsumo), clave, cifras);
    }

    /// <summary>Bajo, Medio y Alto, en el orden del nomenclador; un nivel desconocido va al final.</summary>
    private static int OrdenDelNivel(string nivel)
    {
        var indice = NivelCalorico.Todos.ToList().FindIndex(n => n.Nombre == nivel);
        return indice < 0 ? int.MaxValue : indice;
    }
}
