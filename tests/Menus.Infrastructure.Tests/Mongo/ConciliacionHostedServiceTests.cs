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
}
