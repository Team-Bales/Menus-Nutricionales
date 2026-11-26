using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Al arrancar la API, deja la colección de fichas con su validador y el índice único de RM-08
/// (<see cref="MenusMongoContext.InicializarAsync"/>). Solo se registra si hay MongoDB configurado (#283).
/// </summary>
/// <remarks>
/// <para>
/// Hasta el #283 nadie lo llamaba fuera de las pruebas: en una base nueva, la colección se creaba sin validador ni
/// índice con la primera escritura. Ahora que la API escribe fichas, el índice único es lo que impide dos fichas para
/// el mismo ítem.
/// </para>
/// <para>
/// Corre en segundo plano para no demorar el arranque si MongoDB tarda en responder, y un fallo se registra sin
/// detener la API: la inicialización es idempotente y se vuelve a intentar en el próximo arranque.
/// </para>
/// </remarks>
/// <param name="contexto">El contexto de MongoDB.</param>
/// <param name="logger">El registro del resultado.</param>
public sealed class InicializacionDeFichasHostedService(
    MenusMongoContext contexto,
    ILogger<InicializacionDeFichasHostedService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await contexto.InicializarAsync(stoppingToken);
            logger.LogInformation("Colección de fichas nutricionales lista, con su validador y su índice único.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // La API se detuvo antes de terminar: no hay nada que registrar.
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "No se pudo inicializar la colección de fichas nutricionales ({Tipo}); se reintenta en el próximo arranque.",
                ex.GetType().FullName);
        }
    }
}
