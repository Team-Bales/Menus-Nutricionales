using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Menus.Api.Seguridad;

/// <summary>Nombres de las políticas de límite de peticiones.</summary>
public static class PoliticasDeTasa
{
    /// <summary>Límite de los endpoints de autenticación (login y registro).</summary>
    public const string Autenticacion = "Autenticacion";
}

/// <summary>
/// Configuración del límite de peticiones, en la sección <c>RateLimiting</c> de <c>appsettings</c>.
/// </summary>
public sealed class OpcionesDeLimiteDePeticiones
{
    /// <summary>Nombre de la sección de configuración.</summary>
    public const string Seccion = "RateLimiting";

    /// <summary>Peticiones permitidas por ventana y por origen.</summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>Duración de la ventana, en segundos.</summary>
    public int WindowSeconds { get; set; } = 60;
}

/// <summary>
/// Límite de peticiones de los endpoints de autenticación (#294, §13.3): ventana fija por dirección de origen.
/// </summary>
/// <remarks>
/// Es una defensa básica contra la fuerza bruta y el abuso del login. El límite se cuenta por IP de origen y no por
/// usuario porque el login es anónimo: todavía no hay una identidad que particionar. Detrás de nginx, la IP del
/// cliente llega en <c>X-Forwarded-For</c>; <c>Program</c> configura <c>UseForwardedHeaders</c> para que
/// <see cref="HttpContext.Connection"/> la use. Cuando se supera, la API responde 429 con <c>ProblemDetails</c> y
/// <c>Retry-After</c>, sin revelar nada del sistema.
/// </remarks>
public static class LimiteDePeticiones
{
    /// <summary>
    /// Registra el servicio de límite de peticiones y la política de autenticación.
    /// </summary>
    /// <param name="services">Contenedor de servicios de la API.</param>
    /// <returns>El mismo contenedor, para encadenar.</returns>
    public static IServiceCollection AddLimiteDePeticiones(this IServiceCollection services)
    {
        services.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            opciones.AddPolicy(PoliticasDeTasa.Autenticacion, contexto =>
            {
                var limite = contexto.RequestServices
                    .GetRequiredService<IOptions<OpcionesDeLimiteDePeticiones>>().Value;
                var origen = ClaveDeOrigen(contexto.Connection.RemoteIpAddress);

                return RateLimitPartition.GetFixedWindowLimiter(origen, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limite.PermitLimit,
                    Window = TimeSpan.FromSeconds(limite.WindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                });
            });

            opciones.OnRejected = async (contexto, ct) =>
            {
                var limite = contexto.HttpContext.RequestServices
                    .GetRequiredService<IOptions<OpcionesDeLimiteDePeticiones>>().Value;

                contexto.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                contexto.HttpContext.Response.Headers.RetryAfter =
                    SegundosDeEspera(contexto.Lease, limite.WindowSeconds);

                var problema = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Demasiadas solicitudes. Inténtalo de nuevo más tarde.",
                };

                await contexto.HttpContext.RequestServices
                    .GetRequiredService<IProblemDetailsService>()
                    .TryWriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = contexto.HttpContext,
                        ProblemDetails = problema,
                    });
            };
        });

        return services;
    }

    /// <summary>
    /// Clave de partición del límite: la IP de origen, o un valor fijo cuando la conexión no la informa (por ejemplo,
    /// en las pruebas en memoria).
    /// </summary>
    internal static string ClaveDeOrigen(IPAddress? direccion)
        => direccion?.ToString() ?? "sin-direccion";

    /// <summary>
    /// Segundos que se anuncian en <c>Retry-After</c>: los del arrendamiento si el limitador los informa y, si no, la
    /// ventana completa, que es la espera máxima.
    /// </summary>
    internal static string SegundosDeEspera(RateLimitLease arrendamiento, int ventanaEnSegundos)
        => arrendamiento.TryGetMetadata(MetadataName.RetryAfter, out var espera)
            ? Math.Ceiling(espera.TotalSeconds).ToString(CultureInfo.InvariantCulture)
            : ventanaEnSegundos.ToString(CultureInfo.InvariantCulture);
}
