using Menus.Infrastructure.Mongo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// <see cref="BackgroundService"/> que ejecuta la conciliación RM-10 periódicamente según
/// <see cref="ConciliacionOptions"/>.
/// </summary>
/// <remarks>
/// <para>Ciclo de vida:</para>
/// <list type="number">
/// <item>Espera <see cref="ConciliacionOptions.RetrasoInicial"/> tras el arranque.</item>
/// <item>Ejecuta <see cref="IFichasConciliador.ConciliarAsync"/> dentro de un <see cref="IServiceScope"/>.</item>
/// <item>Repite cada <see cref="ConciliacionOptions.Intervalo"/> usando <see cref="PeriodicTimer"/>.</item>
/// <item>Captura y registra excepciones sin matar el proceso; respeta <see cref="CancellationToken"/>.</item>
/// </list>
/// <para>Solo se registra si <see cref="ConciliacionOptions.Habilitada"/> es <see langword="true"/> y hay MongoDB configurado.</para>
/// </remarks>
/// <param name="proveedorServicios">Proveedor de servicios para crear scopes.</param>
/// <param name="opciones">Opciones de conciliación.</param>
/// <param name="logger">Logger.</param>
public sealed class ConciliacionHostedService(
    IServiceProvider proveedorServicios,
    IOptions<ConciliacionOptions> opciones,
    ILogger<ConciliacionHostedService> logger) : BackgroundService
{
    private readonly IServiceProvider _proveedorServicios = proveedorServicios;
    private readonly ConciliacionOptions _opciones = opciones.Value;
    private readonly ILogger<ConciliacionHostedService> _logger = logger;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opciones.Habilitada)
        {
            _logger.LogInformation("Conciliación deshabilitada (Conciliacion:Habilitada=false); el servicio no se ejecuta.");
            return;
        }

        _logger.LogInformation(
            "Conciliación RM-10 iniciada: retraso inicial={Retraso}, intervalo={Intervalo}, tamaño de lote={TamanoLote}",
            _opciones.RetrasoInicial, _opciones.Intervalo, _opciones.TamanoLote);

        try
        {
            // Esperar el retraso inicial
            await Task.Delay(_opciones.RetrasoInicial, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Conciliación cancelada durante el retraso inicial.");
            return;
        }

        // Primera ejecución
        await EjecutarConciliacionAsync(stoppingToken);

        // Ejecuciones periódicas
        using var timer = new PeriodicTimer(_opciones.Intervalo);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Conciliación cancelada; deteniendo servicio.");
                return;
            }

            await EjecutarConciliacionAsync(stoppingToken);
        }
    }

    private async Task EjecutarConciliacionAsync(CancellationToken cancellationToken)
    {
        using var scope = _proveedorServicios.CreateScope();
        var conciliador = scope.ServiceProvider.GetRequiredService<IFichasConciliador>();

        try
        {
            var resultado = await conciliador.ConciliarAsync(cancellationToken);

            _logger.LogInformation(
                "Conciliación completada: Revisadas={Revisadas}, HuerfanasEliminadas={Huerfanas}, NombresActualizados={Nombres}, FichasInvalidas={Invalidas}, DivergenciasRm06={Divergencias}",
                resultado.Revisadas, resultado.HuerfanasEliminadas, resultado.NombresActualizados,
                resultado.FichasInvalidas, resultado.DivergenciasRm06);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Conciliación cancelada por token de parada.");
            throw; // Re-lanzar para que el bucle principal se detenga
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en la conciliación RM-10; se reintentará en el siguiente ciclo.");
            // No re-lanzar: el servicio sigue vivo y reintentará en el siguiente tick
        }
    }
}
