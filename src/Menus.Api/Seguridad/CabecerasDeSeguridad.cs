using System.Globalization;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Options;

namespace Menus.Api.Seguridad;

/// <summary>
/// Cabeceras de seguridad que acompañan a todas las respuestas de la API (#294, §13.3).
/// </summary>
/// <remarks>
/// <para>
/// La API solo devuelve JSON, así que su política de contenido puede ser la más restrictiva: nada de scripts, estilos
/// ni marcos.
/// </para>
/// <para>
/// Las cabeceras se registran en <see cref="HttpResponse.OnStarting"/> y no se escriben antes de llamar al resto de la
/// tubería: el manejador de excepciones limpia la respuesta con <c>Response.Clear()</c> y borraría las cabeceras ya
/// escritas. Así también las llevan los 401, 403, 404, 409, 422 y 500, además de la comprobación previa de CORS.
/// </para>
/// <para>
/// HSTS se escribe aquí con las opciones de <see cref="HstsOptions"/>, en lugar de usar <c>UseHsts</c>, por la misma
/// razón: <c>UseHsts</c> escribe la cabecera antes de llamar al resto de la tubería y <c>Response.Clear()</c> la
/// borraría en las respuestas de error. Solo se emite sobre HTTPS, fuera de desarrollo y para hosts no excluidos.
/// El cliente Blazor recibe las suyas desde <c>nginx.conf</c>.
/// </para>
/// </remarks>
public static class CabecerasDeSeguridad
{
    /// <summary>
    /// Política de seguridad de contenido de la API: no se carga ningún recurso y no se puede incrustar en un marco.
    /// </summary>
    public const string PoliticaDeContenido =
        "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    /// <summary>
    /// Agrega las cabeceras de seguridad a cada respuesta.
    /// </summary>
    /// <param name="app">Constructor de la tubería de la aplicación.</param>
    /// <returns>El mismo constructor, para encadenar.</returns>
    public static IApplicationBuilder UseCabecerasDeSeguridad(this IApplicationBuilder app)
    {
        var entorno = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        var hsts = app.ApplicationServices.GetRequiredService<IOptions<HstsOptions>>().Value;
        var valorHsts = ArmarHsts(hsts);

        return app.Use((contexto, siguiente) =>
        {
            contexto.Response.OnStarting(() =>
            {
                var cabeceras = contexto.Response.Headers;
                cabeceras["X-Content-Type-Options"] = "nosniff";
                cabeceras["X-Frame-Options"] = "DENY";
                cabeceras["Referrer-Policy"] = "no-referrer";
                cabeceras["Content-Security-Policy"] = PoliticaDeContenido;
                cabeceras["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

                if (!entorno.IsDevelopment()
                    && contexto.Request.IsHttps
                    && !hsts.ExcludedHosts.Contains(contexto.Request.Host.Host, StringComparer.OrdinalIgnoreCase))
                {
                    cabeceras.StrictTransportSecurity = valorHsts;
                }

                return Task.CompletedTask;
            });

            return siguiente(contexto);
        });
    }

    private static string ArmarHsts(HstsOptions opciones)
    {
        var maxAge = Convert.ToInt64(Math.Floor(opciones.MaxAge.TotalSeconds))
            .ToString(CultureInfo.InvariantCulture);
        var valor = $"max-age={maxAge}";

        if (opciones.IncludeSubDomains)
        {
            valor += "; includeSubDomains";
        }

        if (opciones.Preload)
        {
            valor += "; preload";
        }

        return valor;
    }
}
