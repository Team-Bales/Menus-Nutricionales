using System.Globalization;
using System.Text;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Application.IA.Proveedores;

/// <summary>
/// Fuente <c>aceptacion</c> (#325, contrato de IA §4): la tasa de aceptación global y por nivel calórico, y los ítems
/// más rechazados (5 como máximo).
/// </summary>
/// <remarks>
/// <para>
/// <b>Qué consumos cuenta:</b> los del paciente de la valoración en el menú valorado, servidos hasta el día de la
/// valoración (decisión del #325). Es lo que la valoración juzga; los consumos posteriores no la explican.
/// </para>
/// <para>
/// No lleva datos del paciente: solo su referencia opaca. Los nombres de los ítems y los niveles calóricos son del
/// catálogo, y pasan tal cual (§5.1).
/// </para>
/// <para>
/// Las tasas van como porcentaje entero con sus conteos («72 % (18 de 25)»), y cada número del texto está en
/// <see cref="BloqueDeContexto.Cifras"/> (§6).
/// </para>
/// </remarks>
/// <param name="valoraciones">Repositorio de valoraciones.</param>
/// <param name="menus">Repositorio de menús: el agregado trae asignaciones, servicios y consumos.</param>
/// <param name="items">Repositorio de ítems, para el nombre y el nivel calórico.</param>
internal sealed class ProveedorDeAceptacion(
    IValoracionRepository valoraciones,
    IMenuRepository menus,
    IItemAlimentarioRepository items) : IProveedorDeContexto
{
    /// <summary>Nombre de la fuente.</summary>
    public const string NombreFuente = "aceptacion";

    /// <summary>Versión del esquema del bloque.</summary>
    public const int VersionDelBloque = 1;

    /// <summary>Cuántos ítems rechazados se informan como máximo.</summary>
    public const int MaximoDeRechazados = 5;

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
        if (valoracion is null || menu is null)
        {
            return null;
        }

        var hasta = DateOnly.FromDateTime(valoracion.FechaValoracion);
        var consumos = menu.Asignaciones
            .Where(a => a.PacienteId == valoracion.PacienteId)
            .SelectMany(a => a.Servicios)
            .Where(s => s.FechaServicio <= hasta)
            .SelectMany(s => s.Consumos)
            .ToList();
        if (consumos.Count == 0)
        {
            return null;
        }

        var catalogo = (await items.GetByIdsAsync([.. consumos.Select(c => c.ItemId).Distinct()], ct))
            .ToDictionary(i => i.Id);
        var referencia = caso.ObtenerCodigoPaciente(valoracion.PacienteId) ?? "el paciente";

        var texto = new StringBuilder();
        var cifras = new Dictionary<string, double>();

        Global(consumos, referencia, texto, cifras);
        PorNivel(consumos, catalogo, texto, cifras);
        MasRechazados(consumos, catalogo, texto, cifras);

        return new ResultadoDeContexto(
            new BloqueDeContexto(NombreFuente, VersionDelBloque, texto.ToString(), cifras),
            []);
    }

    private static void Global(List<Consumo> consumos, string referencia, StringBuilder texto, Dictionary<string, double> cifras)
    {
        var aceptados = consumos.Count(c => c.Aceptacion);
        texto.Append(CultureInfo.InvariantCulture,
            $"Aceptación {TextoDeBloque.DeLaReferencia(referencia)} en el menú valorado: aceptó el "
            + $"{TextoDeBloque.PorcentajeConConteos(aceptados, consumos.Count)} de los ítems servidos.");
        Registrar(cifras, "aceptacion.global", aceptados, consumos.Count);
    }

    private static void PorNivel(
        List<Consumo> consumos,
        IReadOnlyDictionary<Guid, ItemAlimentario> catalogo,
        StringBuilder texto,
        Dictionary<string, double> cifras)
    {
        var partes = new List<string>();
        foreach (var nivel in NivelCalorico.Todos)
        {
            var delNivel = consumos
                .Where(c => catalogo.TryGetValue(c.ItemId, out var item) && item.NivelCaloricoId == nivel.Id)
                .ToList();
            if (delNivel.Count == 0)
            {
                continue;
            }

            var aceptados = delNivel.Count(c => c.Aceptacion);
            partes.Add($"{nivel.Nombre} {TextoDeBloque.PorcentajeConConteos(aceptados, delNivel.Count)}");
            Registrar(cifras, $"aceptacion.nivel.{nivel.Nombre.ToLowerInvariant()}", aceptados, delNivel.Count);
        }

        if (partes.Count > 0)
        {
            texto.Append($" Por nivel calórico: {string.Join("; ", partes)}.");
        }
    }

    /// <summary>
    /// Los ítems con al menos un rechazo, de más a menos rechazos; a igual cantidad, el de mayor tasa de rechazo, y
    /// después por nombre.
    /// </summary>
    private static void MasRechazados(
        List<Consumo> consumos,
        IReadOnlyDictionary<Guid, ItemAlimentario> catalogo,
        StringBuilder texto,
        Dictionary<string, double> cifras)
    {
        var rechazados = consumos
            .Where(c => catalogo.ContainsKey(c.ItemId))
            .GroupBy(c => c.ItemId)
            .Select(g => (Item: catalogo[g.Key], Rechazos: g.Count(c => !c.Aceptacion), Servidos: g.Count()))
            .Where(x => x.Rechazos > 0)
            .OrderByDescending(x => x.Rechazos)
            .ThenByDescending(x => (decimal)x.Rechazos / x.Servidos)
            .ThenBy(x => x.Item.Nombre, StringComparer.CurrentCulture)
            .Take(MaximoDeRechazados)
            .ToList();

        if (rechazados.Count == 0)
        {
            texto.Append(" No hay ítems rechazados.");
            return;
        }

        var partes = new List<string>();
        for (var i = 0; i < rechazados.Count; i++)
        {
            var (item, rechazos, servidos) = rechazados[i];
            var nivel = NivelCalorico.Todos.FirstOrDefault(n => n.Id == item.NivelCaloricoId)?.Nombre;
            var conNivel = nivel is null ? item.Nombre : $"{item.Nombre} (nivel {nivel})";
            partes.Add($"{conNivel}, rechazado {rechazos} de {servidos} veces");
            cifras[$"aceptacion.rechazado.{i + 1}.rechazos"] = rechazos;
            cifras[$"aceptacion.rechazado.{i + 1}.servidos"] = servidos;
        }

        texto.Append($" Ítems más rechazados: {string.Join("; ", partes)}.");
    }

    private static void Registrar(Dictionary<string, double> cifras, string clave, int parte, int total)
    {
        cifras[$"{clave}.porcentaje"] = TextoDeBloque.Porcentaje(parte, total);
        cifras[$"{clave}.aceptados"] = parte;
        cifras[$"{clave}.total"] = total;
    }
}
