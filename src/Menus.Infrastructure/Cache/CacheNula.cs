using Menus.Application.Comun;

namespace Menus.Infrastructure.Cache;

/// <summary>
/// <see cref="ICache"/> cuando no hay cadena de conexión a Redis configurada (<c>ConnectionStrings:Redis</c>):
/// siempre falla la lectura, y guardar o invalidar no hacen nada. Así la API arranca sin Redis, igual que sin
/// MongoDB, y las dos consultas que cachea se calculan siempre contra PostgreSQL.
/// </summary>
public sealed class CacheNula : ICache
{
    public Task<T?> ObtenerAsync<T>(string clave, CancellationToken ct = default) where T : class
        => Task.FromResult<T?>(null);

    public Task GuardarAsync<T>(string clave, T valor, TimeSpan tiempoDeVida, CancellationToken ct = default) where T : class
        => Task.CompletedTask;

    public Task InvalidarAsync(string clave, CancellationToken ct = default) => Task.CompletedTask;
}
