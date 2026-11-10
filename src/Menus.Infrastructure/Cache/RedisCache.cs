using System.Text.Json;
using Menus.Application.Comun;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Menus.Infrastructure.Cache;

/// <summary>
/// <see cref="ICache"/> sobre Redis (ADR-005). Un fallo de conexión o de tiempo de espera nunca se propaga: se
/// registra en el log y se trata como si la clave no estuviera, para que el llamador calcule el valor contra
/// PostgreSQL (informe, «Consultas recurrentes»: se pierde latencia, no corrección).
/// </summary>
/// <param name="conexion">Multiplexor de conexión a Redis, compartido por toda la aplicación.</param>
/// <param name="logger">Log de los fallos de Redis.</param>
public sealed class RedisCache(IConnectionMultiplexer conexion, ILogger<RedisCache> logger) : ICache
{
    public async Task<T?> ObtenerAsync<T>(string clave, CancellationToken ct = default) where T : class
    {
        try
        {
            var valor = await conexion.GetDatabase().StringGetAsync(clave);
            return valor.IsNullOrEmpty ? null : JsonSerializer.Deserialize<T>((string)valor!);
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException or TimeoutException)
        {
            logger.LogWarning(ex, "No se pudo leer Redis para la clave {Clave}; se calcula contra PostgreSQL.", clave);
            return null;
        }
    }

    public async Task GuardarAsync<T>(string clave, T valor, TimeSpan tiempoDeVida, CancellationToken ct = default) where T : class
    {
        try
        {
            await conexion.GetDatabase().StringSetAsync(clave, JsonSerializer.Serialize(valor), tiempoDeVida);
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException or TimeoutException)
        {
            logger.LogWarning(ex, "No se pudo escribir en Redis la clave {Clave}.", clave);
        }
    }

    public async Task InvalidarAsync(string clave, CancellationToken ct = default)
    {
        try
        {
            await conexion.GetDatabase().KeyDeleteAsync(clave);
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException or TimeoutException)
        {
            logger.LogWarning(ex, "No se pudo invalidar en Redis la clave {Clave}; expirará por su tiempo de vida.", clave);
        }
    }
}
