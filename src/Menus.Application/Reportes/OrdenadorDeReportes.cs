using System.Linq.Expressions;
using System.Reflection;

namespace Menus.Application.Reportes;

/// <summary>
/// Utilidad genérica para ordenar listas en memoria con selectores tipados.
/// El orden canónico SQL se conserva como desempate gracias a la estabilidad de OrderBy/OrderByDescending.
/// </summary>
internal static class OrdenadorDeReportes
{
    /// <summary>
    /// Aplica ordenamiento a una lista ya materializada, usando un selector tipado por nombre de propiedad.
    /// </summary>
    /// <typeparam name="T">Tipo de los elementos de la lista.</typeparam>
    /// <param name="datos">Lista a ordenar (se modifica in-place y se devuelve para encadenar).</param>
    /// <param name="orden">Especificación de columna y dirección; si es null o sin columna, conserva el orden actual.</param>
    /// <returns>La misma lista, ordenada según la especificación.</returns>
    /// <exception cref="ArgumentException">Si la columna no existe en el tipo T.</exception>
    public static IReadOnlyList<T> AplicarOrden<T>(this IReadOnlyList<T> datos, OrdenDeReporte? orden)
        where T : class
    {
        if (orden is null || !orden.TieneOrden)
        {
            return datos;
        }

        var propiedad = typeof(T).GetProperty(orden.Columna!, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (propiedad is null)
        {
            throw new ArgumentException($"La columna '{orden.Columna}' no existe en el tipo {typeof(T).Name}.");
        }

        var parametro = Expression.Parameter(typeof(T), "x");
        var acceso = Expression.Property(parametro, propiedad);
        var lambda = Expression.Lambda(acceso, parametro);

        var metodo = orden.DireccionNormalizada == "desc"
            ? nameof(Enumerable.OrderByDescending)
            : nameof(Enumerable.OrderBy);

        var queryable = datos.AsQueryable();
        var resultado = queryable.Provider.CreateQuery<T>(
            Expression.Call(
                typeof(Queryable),
                metodo,
                [typeof(T), propiedad.PropertyType],
                queryable.Expression,
                Expression.Quote(lambda)));

        return [.. resultado];
    }
}
