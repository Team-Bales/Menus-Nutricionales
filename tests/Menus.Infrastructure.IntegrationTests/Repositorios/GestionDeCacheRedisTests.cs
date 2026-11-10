using System;
using System.Threading.Tasks;
using Menus.Infrastructure.Cache;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>Pruebas contra Redis real de <see cref="RedisCache"/> (#101): guardar, leer, invalidar y expirar.</summary>
[Collection("Redis")]
public sealed class GestionDeCacheRedisTests(RedisFixture fixture)
{
    private RedisCache Cache() => new(fixture.Conexion, NullLogger<RedisCache>.Instance);

    private static string NuevaClave() => $"test:{Guid.NewGuid():N}";

    [Fact]
    public async Task ObtenerAsync_de_una_clave_que_no_existe_es_un_fallo_de_cache()
        => Assert.Null(await Cache().ObtenerAsync<string>(NuevaClave()));

    [Fact]
    public async Task GuardarAsync_y_luego_ObtenerAsync_devuelve_el_mismo_valor()
    {
        var clave = NuevaClave();
        var valor = new ValorDePrueba("Ana", 30);

        await Cache().GuardarAsync(clave, valor, TimeSpan.FromMinutes(5));
        var recuperado = await Cache().ObtenerAsync<ValorDePrueba>(clave);

        Assert.Equal(valor, recuperado);
    }

    [Fact]
    public async Task InvalidarAsync_quita_la_clave()
    {
        var clave = NuevaClave();
        await Cache().GuardarAsync(clave, "valor", TimeSpan.FromMinutes(5));

        await Cache().InvalidarAsync(clave);

        Assert.Null(await Cache().ObtenerAsync<string>(clave));
    }

    [Fact]
    public async Task InvalidarAsync_de_una_clave_que_no_existe_no_lanza()
        => await Cache().InvalidarAsync(NuevaClave());

    [Fact]
    public async Task GuardarAsync_respeta_el_tiempo_de_vida()
    {
        var clave = NuevaClave();
        await Cache().GuardarAsync(clave, "valor", TimeSpan.FromMilliseconds(200));

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.Null(await Cache().ObtenerAsync<string>(clave));
    }

    [Fact]
    public async Task Un_fallo_de_conexion_se_trata_como_fallo_de_cache_y_no_lanza()
    {
        var opciones = ConfigurationOptions.Parse("127.0.0.1:1"); // puerto que nadie escucha
        opciones.AbortOnConnectFail = false;
        opciones.ConnectTimeout = 200;
        await using var conexionRota = await ConnectionMultiplexer.ConnectAsync(opciones);
        var cacheRota = new RedisCache(conexionRota, NullLogger<RedisCache>.Instance);
        var clave = NuevaClave();

        Assert.Null(await cacheRota.ObtenerAsync<string>(clave));
        await cacheRota.GuardarAsync(clave, "valor", TimeSpan.FromMinutes(1));
        await cacheRota.InvalidarAsync(clave);
    }

    private sealed record ValorDePrueba(string Nombre, int Edad);
}
