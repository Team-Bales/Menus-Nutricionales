using System.Threading.Tasks;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Fixture de xUnit que levanta un contenedor Redis 7 (la imagen de <c>docker-compose.yml</c>) con Testcontainers.
/// </summary>
/// <remarks>
/// El contenedor se comparte por colección (ver <see cref="RedisCollection"/>). Cada prueba usa su propia clave
/// (un GUID), así que no hace falta limpiar la base entre pruebas.
/// </remarks>
public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _contenedor = new RedisBuilder().WithImage("redis:7-alpine").Build();

    /// <summary>Conexión multiplexada al contenedor, compartida por toda la colección.</summary>
    public IConnectionMultiplexer Conexion { get; private set; } = null!;

    /// <summary>Arranca el contenedor y conecta.</summary>
    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();
        Conexion = await ConnectionMultiplexer.ConnectAsync(_contenedor.GetConnectionString());
    }

    /// <summary>Cierra la conexión y detiene el contenedor.</summary>
    public async Task DisposeAsync()
    {
        await Conexion.DisposeAsync();
        await _contenedor.DisposeAsync();
    }
}
