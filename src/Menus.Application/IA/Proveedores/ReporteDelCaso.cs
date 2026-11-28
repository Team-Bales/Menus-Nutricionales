using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.IA.Proveedores;

/// <summary>
/// La valoración del caso y su menú, de donde los proveedores de reportes (#335) toman los parámetros de cada
/// consulta informacional.
/// </summary>
/// <remarks>
/// La API de IA recibe una valoración y nada más (contrato de IA, §11.1), así que el menú valorado, su dieta y el
/// paciente son lo único que tienen los reportes para elegir qué consultar.
/// </remarks>
/// <param name="Valoracion">La valoración del caso.</param>
/// <param name="Menu">El menú valorado.</param>
internal sealed record ReporteDelCaso(ValoracionNutricional Valoracion, Menu Menu)
{
    /// <summary>Carga la valoración del caso y su menú.</summary>
    /// <param name="caso">El caso.</param>
    /// <param name="valoraciones">Repositorio de valoraciones.</param>
    /// <param name="menus">Repositorio de menús.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>
    /// La valoración y su menú, o <see langword="null"/> si el caso no tiene valoración o si la valoración o el menú no
    /// existen.
    /// </returns>
    public static async Task<ReporteDelCaso?> CargarAsync(
        ContextoDeCaso caso,
        IValoracionRepository valoraciones,
        IMenuRepository menus,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(caso);
        if (caso.ValoracionId is not { } valoracionId
            || await valoraciones.GetByIdAsync(valoracionId, ct) is not { } valoracion)
        {
            return null;
        }

        return await menus.GetByIdAsync(valoracion.MenuId, ct) is { } menu ? new ReporteDelCaso(valoracion, menu) : null;
    }

    /// <summary>
    /// Arma el resultado del bloque, o <see langword="null"/> si no quedó ninguna parte con datos.
    /// </summary>
    /// <param name="fuente">Fuente del bloque.</param>
    /// <param name="partes">Las oraciones del resumen.</param>
    /// <param name="cifras">Las cifras del bloque.</param>
    /// <returns>El resultado, sin advertencias.</returns>
    public static ResultadoDeContexto? Resultado(string fuente, IReadOnlyList<string> partes, IReadOnlyDictionary<string, double> cifras)
        => partes.Count == 0
            ? null
            : new ResultadoDeContexto(new BloqueDeContexto(fuente, 1, string.Join(' ', partes), cifras), []);
}
