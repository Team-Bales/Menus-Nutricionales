using Menus.Application.Reportes;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.IA.Proveedores;

/// <summary>
/// Fuente <c>reporte-3</c> (#335): el dictamen del menú valorado y los dictámenes del revisor que lo registró
/// (consulta 3).
/// </summary>
/// <remarks>
/// <para>
/// Del menú valorado dice si se aprobó o se rechazó, cuándo, sus observaciones y si se generó para reemplazar un menú
/// rechazado. Del revisor, cuántos dictámenes registró y qué parte fueron aprobaciones, sin nombrarlo: el nombre del
/// personal no entra (decisión del #335).
/// </para>
/// <para>
/// Las observaciones son texto libre del revisor. Entran porque explican el dictamen, en una línea y con 200
/// caracteres como máximo; el <c>AuditorDePrompt</c> (#326) es la red de seguridad.
/// </para>
/// </remarks>
/// <param name="valoraciones">Repositorio de valoraciones.</param>
/// <param name="menus">Repositorio de menús.</param>
/// <param name="consultas">Consultas informacionales (#90).</param>
internal sealed class ProveedorDeDictamenes(
    IValoracionRepository valoraciones,
    IMenuRepository menus,
    IConsultasInformacionales consultas) : IProveedorDeContexto
{
    /// <summary>Nombre de la fuente.</summary>
    public const string NombreFuente = "reporte-3";

    /// <summary>Largo máximo de las observaciones.</summary>
    internal const int LargoDeObservaciones = 200;

    /// <inheritdoc />
    public string Fuente => NombreFuente;

    /// <inheritdoc />
    public async Task<ResultadoDeContexto?> ConstruirAsync(ContextoDeCaso caso, CancellationToken ct = default)
    {
        if (await ReporteDelCaso.CargarAsync(caso, valoraciones, menus, ct) is not { } reporte
            || reporte.Menu.Validacion is not { } dictamen)
        {
            return null;
        }

        var cifras = new Dictionary<string, double>(StringComparer.Ordinal);
        var fecha = TextoDeBloque.FechaRelativa(
            DateOnly.FromDateTime(dictamen.FechaValidacion), caso.FechaReferencia, "reporte-3.dictamen.dias", cifras);
        var texto = $"Dictamen del menú valorado: {(dictamen.Aprobado ? "aprobado" : "rechazado")} {fecha}";
        if (!string.IsNullOrWhiteSpace(dictamen.Observaciones))
        {
            var observaciones = TextoDeBloque.TextoLibre(dictamen.Observaciones, LargoDeObservaciones);
            TextoDeBloque.NumerosDelTexto(observaciones, "reporte-3.dictamen.observaciones", cifras);
            texto += $", con las observaciones «{observaciones}»";
        }

        var partes = new List<string> { texto + "." };
        if (reporte.Menu.ValidacionOrigenId is not null)
        {
            partes.Add("El menú valorado se generó para reemplazar un menú rechazado.");
        }

        var dictamenes = await consultas.ConsultarDictamenesAsync(dictamen.NutricionistaId, ct);
        if (dictamenes.Count > 0)
        {
            var aprobados = dictamenes.Count(d => d.Aprobado);
            partes.Add(
                $"El revisor que lo dictaminó registró {TextoDeBloque.Cantidad(dictamenes.Count, "dictamen", "dictámenes", "reporte-3.revisor.total", cifras)}"
                + $" y aprobó el {TextoDeBloque.PorcentajeConConteos(aprobados, dictamenes.Count, "reporte-3.revisor.aprobados", cifras)}.");
        }

        return ReporteDelCaso.Resultado(NombreFuente, partes, cifras);
    }
}
