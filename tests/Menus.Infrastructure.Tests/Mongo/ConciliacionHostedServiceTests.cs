using Menus.Infrastructure.Mongo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Menus.Infrastructure.Tests.Mongo;

/// <summary>
/// Pruebas de <see cref="ConciliacionHostedService"/> sin contenedores: usan un <see cref="IFichasConciliador"/> falso.
/// </summary>
public sealed class ConciliacionHostedServiceTests
{
    private readonly Mock<IFichasConciliador> _conciliadorMock;
    private readonly Mock<ILogger<ConciliacionHostedService>> _loggerMock;
    private readonly IServiceProvider _proveedorServicios;

    public ConciliacionHostedServiceTests()
    {
        _conciliadorMock = new Mock<IFichasConciliador>();
        _loggerMock = new Mock<ILogger<ConciliacionHostedService>>();

        var servicios = new ServiceCollection();
        servicios.AddSingleton(_conciliadorMock.Object);
        _proveedorServicios = servicios.BuildServiceProvider();
    }

    private ConciliacionHostedService CrearServicio(ConciliacionOptions opciones)
        => new(_proveedorServicios, Options.Create(opciones), _loggerMock.Object);

    /// <summary>
    /// (1) Con <c>RetrasoInicial = 0</c> e <c>Intervalo = 50 ms</c> llama al conciliador al menos 2 veces en 1 s.
    /// </summary>
    [Fact]
    public async Task Periodicidad_llama_al_conciliador_varias_veces()
    {
        var opciones = new ConciliacionOptions
        {
            Habilitada = true,
            RetrasoInicial = TimeSpan.Zero,
            Intervalo = TimeSpan.FromMilliseconds(50),
            TamanoLote = 500
        };

        _conciliadorMock
            .Setup(c => c.ConciliarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoConciliacion(0, 0, 0, 0));

        var servicio = CrearServicio(opciones);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        await servicio.StartAsync(cts.Token);
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
        }
        catch (OperationCanceledException) { }
        await servicio.StopAsync(CancellationToken.None);

        _conciliadorMock.Verify(c => c.ConciliarAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    /// <summary>
    /// (2) Con <c>Habilitada = false</c> no se registra el servicio (el constructor no inicia el bucle).
    /// </summary>
    [Fact]
    public async Task Deshabilitado_no_ejecuta_conciliador()
    {
        var opciones = new ConciliacionOptions
        {
            Habilitada = false,
            RetrasoInicial = TimeSpan.Zero,
            Intervalo = TimeSpan.FromMilliseconds(50)
        };

        var servicio = CrearServicio(opciones);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await servicio.StartAsync(cts.Token);
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);
        }
        catch (OperationCanceledException) { }
        await servicio.StopAsync(CancellationToken.None);

        _conciliadorMock.Verify(c => c.ConciliarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// (3) Si el conciliador lanza, el servicio sigue y vuelve a llamarlo en el siguiente tick.
    /// </summary>
    [Fact]
    public async Task Recuperacion_tras_excepcion_vuelve_a_intentar()
    {
        var opciones = new ConciliacionOptions
        {
            Habilitada = true,
            RetrasoInicial = TimeSpan.Zero,
            Intervalo = TimeSpan.FromMilliseconds(50)
        };

        var llamadas = 0;
        _conciliadorMock
            .Setup(c => c.ConciliarAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                llamadas++;
                if (llamadas == 1)
                {
                    throw new InvalidOperationException("Fallo simulado");
                }
                return Task.FromResult(new ResultadoConciliacion(0, 0, 0, 0));
            });

        var servicio = CrearServicio(opciones);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        await servicio.StartAsync(cts.Token);
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
        }
        catch (OperationCanceledException) { }
        await servicio.StopAsync(CancellationToken.None);

        // Primera llamada falla, siguientes deben seguir
        Assert.True(llamadas >= 2, $"Se esperaban al menos 2 llamadas, hubo {llamadas}");
    }

    /// <summary>
    /// (4) Al cancelar, termina sin excepción.
    /// </summary>
    [Fact]
    public async Task Cancelacion_termina_sin_excepcion()
    {
        var opciones = new ConciliacionOptions
        {
            Habilitada = true,
            RetrasoInicial = TimeSpan.Zero,
            Intervalo = TimeSpan.FromMilliseconds(50)
        };

        _conciliadorMock
            .Setup(c => c.ConciliarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoConciliacion(0, 0, 0, 0));

        var servicio = CrearServicio(opciones);
        using var cts = new CancellationTokenSource();

        await servicio.StartAsync(cts.Token);
        await cts.CancelAsync();

        // StopAsync no debe lanzar aunque el token esté cancelado
        await servicio.StopAsync(CancellationToken.None);

        // Si llegamos aquí sin excepción, la prueba pasa
    }

    /// <summary>
    /// (5) Cancelación durante el retraso inicial (líneas 53-56): si se cancela mientras
    /// espera <see cref="ConciliacionOptions.RetrasoInicial"/>, el servicio registra y sale sin excepción.
    /// </summary>
    [Fact]
    public async Task Cancelacion_durante_retraso_inicial_registra_y_sale()
    {
        var opciones = new ConciliacionOptions
        {
            Habilitada = true,
            RetrasoInicial = TimeSpan.FromSeconds(10), // Largo para poder cancelar
            Intervalo = TimeSpan.FromMilliseconds(50),
            TamanoLote = 500
        };

        _conciliadorMock
            .Setup(c => c.ConciliarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoConciliacion(0, 0, 0, 0));

        // Avisa cuando ExecuteAsync ya arrancó: registra «iniciada» justo antes de esperar el retraso inicial.
        var arranco = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _loggerMock
            .Setup(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("iniciada")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(_ => arranco.TrySetResult()));

        var servicio = CrearServicio(opciones);
        using var cts = new CancellationTokenSource();

        await servicio.StartAsync(cts.Token);

        // Cancelar recién cuando ExecuteAsync está corriendo. Desde .NET 10, BackgroundService lo lanza en otra tarea,
        // y si la cancelación llega antes de que empiece, no corre nada y no hay log que verificar (falló así en el CI
        // del #280: «No invocations performed»).
        await arranco.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cts.CancelAsync();

        // StopAsync no debe lanzar
        await servicio.StopAsync(CancellationToken.None);

        // Verificar que se logueó la cancelación durante el retraso inicial
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("cancelada durante el retraso inicial")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // El conciliador NO debe haber sido llamado
        _conciliadorMock.Verify(c => c.ConciliarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// (6) Cancelación originada por <see cref="IFichasConciliador.ConciliarAsync"/> con token de parada
    /// (líneas 95-98): si el conciliador lanza <see cref="OperationCanceledException"/> y el token
    /// está cancelado, el servicio registra y re-lanza para detener el bucle principal.
    /// </summary>
    [Fact]
    public async Task Cancelacion_desde_conciliador_con_token_parada_relanza_y_detiene_bucle()
    {
        var opciones = new ConciliacionOptions
        {
            Habilitada = true,
            RetrasoInicial = TimeSpan.FromMilliseconds(10),
            Intervalo = TimeSpan.FromMilliseconds(50),
            TamanoLote = 500
        };

        var cts = new CancellationTokenSource();
        var tcsFirstCall = new TaskCompletionSource<ResultadoConciliacion>();
        var segundaLlamadaEmpezo = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;

        // Primera llamada: se completa cuando lo decidamos. Segunda: avisa que empezó y espera la cancelación.
        _conciliadorMock
            .Setup(c => c.ConciliarAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return tcsFirstCall.Task;
                }

                segundaLlamadaEmpezo.TrySetResult();
                return Task.Delay(Timeout.Infinite, ct).ContinueWith<ResultadoConciliacion>(
                    _ => throw new OperationCanceledException(ct), CancellationToken.None);
            });

        var servicio = CrearServicio(opciones);
        await servicio.StartAsync(cts.Token);

        // Completar la primera llamada con éxito; si todavía no empezó, la recibe ya completa.
        tcsFirstCall.SetResult(new ResultadoConciliacion(0, 0, 0, 0));

        // Cancelar recién cuando la segunda llamada está en curso, y no tras un tiempo fijo: en un runner lento, la
        // cancelación llegaba mientras el servicio esperaba el temporizador y no se registraba el log que se verifica.
        await segundaLlamadaEmpezo.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cts.CancelAsync();

        // StopAsync espera a que termine ExecuteAsync, y no debe lanzar.
        await servicio.StopAsync(CancellationToken.None);

        // Verificar que se logueó "cancelada por token de parada"
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("cancelada por token de parada")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // El conciliador fue llamado 2 veces
        Assert.Equal(2, callCount);
    }
}
