using Menus.Application.Reportes;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.IA.Proveedores;

/// <summary>
/// Fuente <c>reporte-6</c> (#335): la correlación entre nivel calórico y resultado nutricional en la dieta del menú
/// valorado, los ítems más rechazados del sistema que tuvieron rechazos en esa dieta y las revalorizaciones del
/// paciente del caso en esa dieta (consulta 6, partes A, B y C).
/// </summary>
/// <remarks>
/// <para>
/// <b>Privacidad.</b> La parte C de la consulta lista a todos los pacientes revalorizados, con nombre, id y sala. Aquí
/// entra solo el paciente del caso (decisión del #335), con su referencia opaca («el paciente» o «P1»): su valor, el
/// promedio de los demás pacientes de su sala en esa dieta y la diferencia. La sala no se nombra (§5.1). El nombre de
/// la dieta se usa para filtrar las partes B y C, pero no se escribe (ver <see cref="ProveedorDeMenusAutomaticos"/>),
/// y tampoco el descriptor de cada ítem.
/// </para>
/// <para>
/// El coeficiente de Spearman va con dos decimales y su signo («-0,45»). El verificador (#327) tiene que leer el signo.
/// </para>
/// </remarks>
/// <param name="valoraciones">Repositorio de valoraciones.</param>
/// <param name="menus">Repositorio de menús.</param>
/// <param name="dietas">Repositorio de dietas, para el nombre con el que filtrar.</param>
/// <param name="consultas">Consultas informacionales (#90).</param>
internal sealed class ProveedorDeCorrelacion(
    IValoracionRepository valoraciones,
    IMenuRepository menus,
    IDietaRepository dietas,
    IConsultasInformacionales consultas) : IProveedorDeContexto
{
    /// <summary>Nombre de la fuente.</summary>
    public const string NombreFuente = "reporte-6";

    /// <summary>Tamaño del ranking de rechazos de la consulta (el mismo que usa la API).</summary>
    internal const int TopDeRechazos = 10;

    private const int MaximoDeRevalorizaciones = 5;

    /// <inheritdoc />
    public string Fuente => NombreFuente;

    /// <inheritdoc />
    public async Task<ResultadoDeContexto?> ConstruirAsync(ContextoDeCaso caso, CancellationToken ct = default)
    {
        if (await ReporteDelCaso.CargarAsync(caso, valoraciones, menus, ct) is not { } reporte
            || await dietas.GetByIdAsync(reporte.Menu.DietaId, ct) is not { } dieta)
        {
            return null;
        }

        var cifras = new Dictionary<string, double>(StringComparer.Ordinal);
        var partes = new List<string>();

        var correlacion = (await consultas.ConsultarCorrelacionPorDietaAsync(null, null, ct))
            .FirstOrDefault(c => c.DietaId == dieta.Id);
        if (correlacion is { MenusValorados: > 0 })
        {
            partes.Add(Correlacion(correlacion, cifras));
        }

        var rechazados = (await consultas.ConsultarItemsMasRechazadosAsync(TopDeRechazos, 1, ct))
            .Where(r => r.Dietas.Split(", ").Contains(dieta.Nombre, StringComparer.Ordinal))
            .ToList();
        if (rechazados.Count > 0)
        {
            partes.Add(Rechazados(rechazados, cifras));
        }

        var revalorizaciones = (await consultas.ConsultarRevalorizadosAsync(ct))
            .Where(r => r.PacienteId == reporte.Valoracion.PacienteId && r.Dieta == dieta.Nombre)
            .Take(MaximoDeRevalorizaciones)
            .ToList();
        if (revalorizaciones.Count > 0)
        {
            var referencia = caso.ObtenerCodigoPaciente(reporte.Valoracion.PacienteId) ?? "el paciente";
            partes.Add(Revalorizaciones(referencia, revalorizaciones, cifras));
        }

        return ReporteDelCaso.Resultado(NombreFuente, partes, cifras);
    }

    private static string Correlacion(CorrelacionPorDietaDto c, Dictionary<string, double> cifras)
    {
        var menusValorados = TextoDeBloque.Cantidad(c.MenusValorados, "menú valorado", "menús valorados", "reporte-6.correlacion.menusValorados", cifras);
        var coeficiente = c.CoeficienteSpearman is { } valor
            ? $"coeficiente de Spearman {TextoDeBloque.Numero((double)valor, 2, "reporte-6.correlacion.spearman", cifras)}, sobre {menusValorados}"
            : $"no se puede calcular con {menusValorados}";
        return $"Correlación entre el nivel calórico de los ítems servidos y el resultado nutricional, en la dieta del menú: {coeficiente}.";
    }

    private static string Rechazados(IReadOnlyList<ItemMasRechazadoDto> filas, Dictionary<string, double> cifras)
    {
        cifras["reporte-6.rechazados.top"] = TopDeRechazos;
        var items = filas.Select((f, i) =>
        {
            var clave = $"reporte-6.rechazados.{i + 1}";
            TextoDeBloque.NumerosDelTexto(f.Item, $"{clave}.nombre", cifras);
            return $"{f.Item} ({f.Clase.ToLowerInvariant()}), rechazado el "
                + TextoDeBloque.PorcentajeConConteos((int)f.Rechazos, (int)f.Consumos, clave, cifras)
                + " de sus consumos";
        });
        return $"De los {TopDeRechazos} ítems con mayor tasa de rechazo en todo el sistema, tuvieron rechazos en la dieta del menú: "
            + $"{string.Join("; ", items)}.";
    }

    private static string Revalorizaciones(string referencia, IReadOnlyList<RevalorizadoDto> filas, Dictionary<string, double> cifras)
    {
        var detalle = filas.Select((r, i) =>
        {
            var clave = $"reporte-6.revalorizacion.{i + 1}";
            var texto = $"valor {TextoDeBloque.Numero(r.ValorRevalorizacion, 0, $"{clave}.valor", cifras)}";
            if (r.PromedioSala is { } promedio)
            {
                texto += $" frente a un promedio de {TextoDeBloque.Numero((double)promedio, 2, $"{clave}.promedio", cifras)}"
                    + " de los demás pacientes de su sala en esa dieta";
                if (r.Diferencia is { } diferencia)
                {
                    texto += $" (diferencia {TextoDeBloque.Numero((double)diferencia, 2, $"{clave}.diferencia", cifras)})";
                }
            }
            else
            {
                texto += ", sin otros pacientes de su sala en esa dieta para comparar";
            }

            return texto;
        });
        return $"Revalorizaciones {TextoDeBloque.DeLaReferencia(referencia)} en la dieta del menú: {string.Join("; ", detalle)}.";
    }
}
