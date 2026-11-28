using Menus.Application.Reportes;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.IA.Proveedores;

/// <summary>
/// Fuente <c>reporte-2</c> (#335): los alimentos simples más usados en los menús finales de la dieta del menú
/// valorado (consulta 2), como máximo 5.
/// </summary>
/// <remarks>
/// De cada alimento dice el nivel calórico, el grupo nutricional, en cuántos menús finales está y si está en el menú
/// valorado: es lo que necesita <c>SugerirAcciones</c> para proponer un cambio con alimentos que la dieta ya usa. La
/// dieta no se nombra (ver <see cref="ProveedorDeMenusAutomaticos"/>).
/// </remarks>
/// <param name="valoraciones">Repositorio de valoraciones.</param>
/// <param name="menus">Repositorio de menús.</param>
/// <param name="consultas">Consultas informacionales (#90).</param>
internal sealed class ProveedorDeAlimentosMasUsados(
    IValoracionRepository valoraciones,
    IMenuRepository menus,
    IConsultasInformacionales consultas) : IProveedorDeContexto
{
    /// <summary>Nombre de la fuente.</summary>
    public const string NombreFuente = "reporte-2";

    private const int MaximoDeAlimentos = 5;

    /// <inheritdoc />
    public string Fuente => NombreFuente;

    /// <inheritdoc />
    public async Task<ResultadoDeContexto?> ConstruirAsync(ContextoDeCaso caso, CancellationToken ct = default)
    {
        if (await ReporteDelCaso.CargarAsync(caso, valoraciones, menus, ct) is not { } reporte)
        {
            return null;
        }

        var filas = await consultas.ConsultarAlimentosMasUsadosAsync(reporte.Menu.DietaId, MaximoDeAlimentos, ct);
        if (filas.Count == 0)
        {
            return null;
        }

        var delMenu = reporte.Menu.Items.Select(i => i.ItemId).ToHashSet();
        var cifras = new Dictionary<string, double>(StringComparer.Ordinal);
        var alimentos = filas.Take(MaximoDeAlimentos).Select((fila, i) =>
        {
            var clave = $"reporte-2.{i + 1}";
            TextoDeBloque.NumerosDelTexto($"{fila.Alimento} {fila.NivelCalorico} {fila.GrupoNutricional}", $"{clave}.nombre", cifras);
            return $"{fila.Alimento} (nivel {fila.NivelCalorico}, grupo {fila.GrupoNutricional}), en "
                + TextoDeBloque.Cantidad(fila.MenusFinales, "menú final", "menús finales", $"{clave}.menusFinales", cifras)
                + (delMenu.Contains(fila.AlimentoId) ? ", está en el menú valorado" : string.Empty);
        });

        var partes = new List<string>
        {
            $"Alimentos simples más usados en los menús finales de la dieta del menú: {string.Join("; ", alimentos)}.",
        };
        return ReporteDelCaso.Resultado(NombreFuente, partes, cifras);
    }
}
