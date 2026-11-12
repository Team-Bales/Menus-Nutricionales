namespace Menus.Application.Comun;

/// <summary>
/// Caché clave-valor en memoria, por delante de PostgreSQL (ADR-005, patrón <i>cache-aside</i>). La única
/// fuente de verdad sigue siendo PostgreSQL: un fallo de la caché nunca debe impedir la respuesta, solo
/// degradar su latencia (informe, «Consultas recurrentes»).
/// </summary>
public interface ICache
{
    /// <summary>
    /// Busca el valor de <paramref name="clave"/>. Si la caché no responde o la clave no está, devuelve
    /// <see langword="null"/>: el llamador calcula el valor contra PostgreSQL.
    /// </summary>
    /// <typeparam name="T">Tipo del valor cacheado.</typeparam>
    /// <param name="clave">Clave del valor.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<T?> ObtenerAsync<T>(string clave, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Guarda <paramref name="valor"/> bajo <paramref name="clave"/>, con <paramref name="tiempoDeVida"/> como
    /// respaldo de la invalidación por evento. Si la caché no responde, no hace nada: la próxima lectura calcula
    /// de nuevo el valor.
    /// </summary>
    /// <typeparam name="T">Tipo del valor a cachear.</typeparam>
    /// <param name="clave">Clave del valor.</param>
    /// <param name="valor">Valor a guardar.</param>
    /// <param name="tiempoDeVida">Tiempo de vida de la clave.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task GuardarAsync<T>(string clave, T valor, TimeSpan tiempoDeVida, CancellationToken ct = default) where T : class;

    /// <summary>Quita <paramref name="clave"/> de la caché. Si no está, no hace nada.</summary>
    /// <param name="clave">Clave a invalidar.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task InvalidarAsync(string clave, CancellationToken ct = default);
}
