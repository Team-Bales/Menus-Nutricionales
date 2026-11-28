using Menus.Application.IA;
using Microsoft.Extensions.Options;

namespace Menus.Api.IA;

/// <summary>Registra de forma auditable la aceptación ya validada de términos para un proveedor externo.</summary>
public sealed class RegistroDeTerminosIAHostedService(
    IOptions<IAOptions> opciones,
    ILogger<RegistroDeTerminosIAHostedService> logger) : IHostedService
{
    private readonly IAOptions _opciones = opciones.Value;
    private readonly ILogger<RegistroDeTerminosIAHostedService> _logger = logger;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (ValidadorDeConfiguracionIA.UsaProveedorExterno(_opciones))
        {
            var terminos = _opciones.Terminos;
            _logger.LogInformation(
                "Términos de IA externa aceptados: Version={Version}, AceptadoPor={AceptadoPor}, Fecha={Fecha}.",
                terminos.Version,
                terminos.AceptadoPor,
                terminos.Fecha);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
