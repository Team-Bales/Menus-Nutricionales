using Menus.Infrastructure.Cache;

namespace Menus.Infrastructure.Tests.Cache;

/// <summary>Pruebas de <see cref="CacheNula"/>: se usa cuando no hay Redis configurado (#101).</summary>
public sealed class CacheNulaTests
{
    private readonly CacheNula _cache = new();

    [Fact]
    public async Task ObtenerAsync_siempre_es_un_fallo_de_cache()
        => Assert.Null(await _cache.ObtenerAsync<string>("clave"));

    [Fact]
    public async Task GuardarAsync_no_hace_nada_y_no_lanza()
        => await _cache.GuardarAsync("clave", "valor", TimeSpan.FromHours(1));

    [Fact]
    public async Task InvalidarAsync_no_hace_nada_y_no_lanza()
        => await _cache.InvalidarAsync("clave");

    [Fact]
    public async Task Guardar_y_luego_obtener_sigue_siendo_un_fallo_de_cache()
    {
        await _cache.GuardarAsync("clave", "valor", TimeSpan.FromHours(1));

        Assert.Null(await _cache.ObtenerAsync<string>("clave"));
    }
}
