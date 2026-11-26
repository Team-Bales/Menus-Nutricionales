using Menus.Application.Catalogo.Fichas;
using Menus.Application.Comun;
using Menus.Infrastructure.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Menus.Infrastructure.Tests.Mongo;

/// <summary>
/// Qué pasa cuando MongoDB está configurado pero no responde (#283): el almacén de fichas responde 503 y la
/// inicialización al arrancar solo lo registra. Sin Docker: el cliente apunta a un puerto donde no escucha nadie, con
/// un tiempo de espera corto.
/// </summary>
public sealed class MongoSinRespuestaTests
{
    private static MenusMongoContext ContextoSinRespuesta(int esperaMs = 300) => new(
        new MongoClient($"mongodb://127.0.0.1:1/?serverSelectionTimeoutMS={esperaMs}&connectTimeoutMS={esperaMs}"),
        "menus_sin_respuesta");

    [Fact]
    public async Task Leer_la_ficha_sin_respuesta_de_MongoDB_responde_servicio_no_disponible()
    {
        var almacen = new AlmacenDeFichasEnMongo(ContextoSinRespuesta());

        var ex = await Assert.ThrowsAsync<ServicioNoDisponibleException>(
            () => almacen.ObtenerAsync(ClaseDeItem.Alimento, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(AlmacenDeFichasEnMongo.MensajeSinRespuesta, ex.Message);
        Assert.IsType<TimeoutException>(ex.InnerException);
    }

    [Fact]
    public async Task Guardar_la_ficha_sin_respuesta_de_MongoDB_responde_servicio_no_disponible()
    {
        var almacen = new AlmacenDeFichasEnMongo(ContextoSinRespuesta());
        var ficha = new FichaNutricionalDto(
            Guid.NewGuid(), "Alimento", "simple", null, null, null, [], null, null, DateTime.UtcNow);

        await Assert.ThrowsAsync<ServicioNoDisponibleException>(() => almacen.GuardarAsync(ficha, CancellationToken.None));
    }

    [Fact]
    public async Task Si_MongoDB_no_responde_al_arrancar_se_registra_una_advertencia_sin_detener_la_API()
    {
        var registro = new RegistroFalso();
        var servicio = new InicializacionDeFichasHostedService(ContextoSinRespuesta(), registro);

        await servicio.InicializarAsync(CancellationToken.None);

        var (nivel, mensaje) = Assert.Single(registro.Entradas);
        Assert.Equal(LogLevel.Warning, nivel);
        Assert.Contains(typeof(TimeoutException).FullName!, mensaje);
    }

    [Fact]
    public async Task Si_la_API_se_detiene_antes_de_inicializar_no_se_registra_nada()
    {
        var registro = new RegistroFalso();
        var servicio = new InicializacionDeFichasHostedService(ContextoSinRespuesta(esperaMs: 30_000), registro);
        using var detenida = new CancellationTokenSource();
        await detenida.CancelAsync();

        await servicio.InicializarAsync(detenida.Token);

        Assert.Empty(registro.Entradas);
    }

    /// <summary>Guarda el nivel y el texto de cada entrada.</summary>
    private sealed class RegistroFalso : ILogger<InicializacionDeFichasHostedService>
    {
        public List<(LogLevel Nivel, string Mensaje)> Entradas { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entradas.Add((logLevel, formatter(state, exception)));
    }
}
