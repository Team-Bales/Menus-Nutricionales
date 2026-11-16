using System;
using System.Linq;
using System.Threading.Tasks;
using Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;
using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.Cache;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Prueba de integración de punta a punta del cache-aside de la disponibilidad calórica (#89; informe, «Consultas
/// recurrentes»): la misma orquestación de <c>DisponibilidadCaloricaHandler</c> contra PostgreSQL y Redis reales a
/// la vez.
/// </summary>
/// <remarks>
/// <para>
/// No pasa por <c>DisponibilidadCaloricaHandler</c> ni por <c>CacheDeDisponibilidadCalorica</c>: son
/// <c>internal</c> de <c>Menus.Application</c>, visibles solo para <c>Menus.Application.Tests</c>. Aquí se repite
/// la misma orquestación —clave de caché, lectura antes de tocar la base, cálculo y guardado— contra la base y la
/// caché reales.
/// </para>
/// <para>
/// No hay forma directa de contar las consultas a PostgreSQL desde fuera, así que la prueba lo infiere: cambia el
/// banco de alimentos por fuera de la orquestación, sin invalidar la caché, y comprueba que una segunda consulta no
/// refleja el cambio porque la sirve Redis, no un recálculo. <see cref="RedisCache"/> ya se prueba en aislado
/// contra Redis en <c>GestionDeCacheRedisTests</c> (guardar, leer, invalidar, expirar); esta prueba cubre la
/// integración completa que pide el #89, no el mecanismo de caché por sí solo.
/// </para>
/// </remarks>
[Collection("PostgreSQLyRedis")]
public sealed class DisponibilidadCaloricaConRedisTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private readonly RedisFixture _redis;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _s = null!;
    private RedisCache _cache = null!;

    public DisponibilidadCaloricaConRedisTests(PostgresFixture postgres, RedisFixture redis)
    {
        _postgres = postgres;
        _redis = redis;
    }

    public async Task InitializeAsync()
    {
        await _postgres.ReiniciarAsync();
        _conexion = await _postgres.AbrirAsync();
        _s = new Sembrador(_conexion);
        _cache = new RedisCache(_redis.Conexion, NullLogger<RedisCache>.Instance);
    }

    public async Task DisposeAsync() => await _conexion.DisposeAsync();

    private static string Clave(Guid dietaId) => $"dieta:{dietaId}:disponibilidad-calorica";

    /// <summary>Misma orquestación que <c>DisponibilidadCaloricaHandler.HandleAsync</c>, con repositorios reales.</summary>
    private async Task<DisponibilidadCaloricaDto> ConsultarAsync(Guid dietaId)
    {
        var clave = Clave(dietaId);
        var cacheado = await _cache.ObtenerAsync<DisponibilidadCaloricaDto>(clave);
        if (cacheado is not null)
        {
            return cacheado;
        }

        await using var ctx = _postgres.CrearContexto();
        IDietaRepository dietas = new DietaRepository(ctx);
        IItemAlimentarioRepository items = new ItemAlimentarioRepository(ctx);
        IMenuRepository menus = new MenuRepository(ctx);

        var gruposProhibidos = await dietas.GetGruposProhibidosAsync(dietaId);
        var delBanco = await items.ContarPorNivelCaloricoAsync(gruposProhibidos);
        var deMenusFinales = await menus.ContarItemsFinalesPorNivelCaloricoAsync(dietaId);

        var porNivel = NivelCalorico.Todos
            .Select(n => new DisponibilidadPorNivelDto(n.Id, n.Nombre, delBanco.GetValueOrDefault(n.Id), deMenusFinales.GetValueOrDefault(n.Id)))
            .ToList();

        var resultado = new DisponibilidadCaloricaDto(dietaId, porNivel);
        await _cache.GuardarAsync(clave, resultado, TimeSpan.FromHours(1)); // mismo TTL que CacheDeDisponibilidadCalorica.TiempoDeVida
        return resultado;
    }

    private static int ItemsDelBancoEnNivelMedio(DisponibilidadCaloricaDto disponibilidad)
        => disponibilidad.PorNivel.Single(p => p.NivelCaloricoId == NivelCalorico.Medio.Id).ItemsDelBanco;

    [Fact]
    public async Task Una_segunda_consulta_no_recalcula_contra_postgres_sino_que_la_sirve_la_cache()
    {
        var dieta = await _s.DietaAsync();
        await _s.AlimentoAsync(); // un ítem de nivel Medio, para contar

        var primera = await ConsultarAsync(dieta);
        var conteoPrimera = ItemsDelBancoEnNivelMedio(primera);

        // Cambia el banco por fuera de la orquestación, sin invalidar la caché: si la segunda consulta recalculara
        // contra PostgreSQL, este conteo subiría.
        await _s.AlimentoAsync();

        var segunda = await ConsultarAsync(dieta);
        Assert.Equal(conteoPrimera, ItemsDelBancoEnNivelMedio(segunda));

        // Control: invalidada la caché, la tercera consulta sí refleja el ítem agregado.
        await _cache.InvalidarAsync(Clave(dieta));

        var tercera = await ConsultarAsync(dieta);
        Assert.Equal(conteoPrimera + 1, ItemsDelBancoEnNivelMedio(tercera));
    }
}
