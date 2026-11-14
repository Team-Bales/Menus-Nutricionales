using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.ValueObjects;

namespace Menus.Application.GeneracionDeMenus;

/// <summary>
/// Estrategia por defecto de <see cref="IEstrategiaGeneracionMenu"/> (#98): reparte la cantidad total entre los
/// tipos de preparación según sus proporciones, con el método del resto mayor para que la suma coincida
/// exactamente, y dentro de cada tipo elige al azar entre los ítems compatibles, priorizando los de los grupos
/// que el menú debe cubrir (informe, RF-10).
/// </summary>
public sealed class EstrategiaGeneracionAleatoria : IEstrategiaGeneracionMenu
{
    public IReadOnlyList<Guid> SeleccionarItems(
        IReadOnlyList<ItemAlimentario> itemsCompatibles,
        DistribucionTiposPreparacion distribucion,
        CoberturaGruposNutricionales cobertura,
        CantidadTotalAlimentos cantidadTotal)
    {
        var gruposCubiertos = new HashSet<Guid>(cobertura.GrupoIds);
        var seleccionados = new List<Guid>();

        foreach (var (tipoId, cantidad) in RepartirPorTipo(distribucion, cantidadTotal))
        {
            var delTipo = itemsCompatibles.Where(i => i.TipoPreparacionId == tipoId).ToList();
            if (delTipo.Count < cantidad)
            {
                throw new ReglaDeNegocioException(
                    $"No hay suficientes ítems compatibles con la dieta del tipo de preparación {tipoId}: " +
                    $"se necesitan {cantidad} y hay {delTipo.Count}.");
            }

            var prioritarios = delTipo.Where(i => gruposCubiertos.Contains(i.GrupoNutricionalId)).OrderBy(_ => Random.Shared.Next());
            var resto = delTipo.Where(i => !gruposCubiertos.Contains(i.GrupoNutricionalId)).OrderBy(_ => Random.Shared.Next());

            seleccionados.AddRange(prioritarios.Concat(resto).Take(cantidad).Select(i => i.Id));
        }

        return seleccionados;
    }

    /// <summary>
    /// Reparte <paramref name="cantidadTotal"/> entre los tipos de preparación de <paramref name="distribucion"/>
    /// según sus proporciones. Usa el método del resto mayor (apportionment): cada tipo recibe el piso de su
    /// cuota exacta, y los puestos que faltan para llegar al total se asignan, de a uno, a los tipos con mayor
    /// resto. Así la suma siempre coincide exactamente con el total, aunque las proporciones no den números enteros.
    /// </summary>
    private static List<(Guid TipoPreparacionId, int Cantidad)> RepartirPorTipo(
        DistribucionTiposPreparacion distribucion,
        CantidadTotalAlimentos cantidadTotal)
    {
        var total = cantidadTotal.Valor;

        var cuotas = distribucion.Proporciones
            .Select(p => (p.TipoPreparacionId, Exacta: p.Valor * total))
            .Select(c => (c.TipoPreparacionId, Piso: (int)Math.Floor(c.Exacta), Resto: c.Exacta - Math.Floor(c.Exacta)))
            .ToArray();

        var cantidades = cuotas.ToDictionary(c => c.TipoPreparacionId, c => c.Piso);

        var faltante = total - cuotas.Sum(c => c.Piso);
        var porMayorResto = cuotas.OrderByDescending(c => c.Resto).Select(c => c.TipoPreparacionId).ToList();
        for (var i = 0; i < faltante; i++)
        {
            cantidades[porMayorResto[i]]++;
        }

        return [.. cantidades.Select(kv => (kv.Key, kv.Value))];
    }
}
