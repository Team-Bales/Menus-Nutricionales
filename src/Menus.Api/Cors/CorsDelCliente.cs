using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Menus.Api.Cors;

/// <summary>
/// Política CORS para el cliente Blazor cuando corre con <c>dotnet run</c>, en otro origen que la API.
/// </summary>
/// <remarks>
/// Los orígenes solo se configuran en <c>appsettings.Development.json</c>. Sin orígenes, la política no admite a
/// ninguno: en Docker el cliente llega a la API por el proxy de Nginx, desde el mismo origen, y no usa CORS.
/// </remarks>
public static class CorsDelCliente
{
    /// <summary>Nombre de la política.</summary>
    public const string Politica = "Cliente";

    /// <summary>Clave de configuración con los orígenes admitidos.</summary>
    public const string Seccion = "Cors:OrigenesDelCliente";

    /// <summary>Registra la política con los orígenes de la configuración.</summary>
    /// <param name="services">Contenedor de servicios.</param>
    /// <param name="configuracion">Configuración de la aplicación.</param>
    /// <returns>El mismo contenedor, para encadenar registros.</returns>
    public static IServiceCollection AddCorsDelCliente(this IServiceCollection services, IConfiguration configuracion)
    {
        var origenes = configuracion.GetSection(Seccion).Get<string[]>() ?? [];

        // Cualquier cabecera: el cliente manda X-Actor-Id, y desde el #74 mandará Authorization.
        var politica = new CorsPolicyBuilder(origenes).AllowAnyHeader().AllowAnyMethod().Build();

        return services.AddCors(o => o.AddPolicy(Politica, politica));
    }
}
