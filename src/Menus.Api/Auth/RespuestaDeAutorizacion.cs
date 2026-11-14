using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Auth;

/// <summary>
/// Responde los 401 y 403 de la autorización con un <see cref="ProblemDetails"/>, como el resto de los errores de
/// la API (ADR-010, §4), en lugar de una respuesta sin cuerpo (#74).
/// </summary>
/// <remarks>
/// Primero deja que el esquema JWT haga lo suyo (<c>ChallengeAsync</c> pone el 401 y la cabecera
/// <c>WWW-Authenticate: Bearer</c>; <c>ForbidAsync</c>, el 403) y después escribe el cuerpo. Si la petición está
/// autorizada, sigue como siempre.
/// </remarks>
/// <param name="problemDetails">Servicio que escribe el cuerpo <c>application/problem+json</c>.</param>
public sealed class RespuestaDeAutorizacion(IProblemDetailsService problemDetails) : IAuthorizationMiddlewareResultHandler
{
    /// <summary>Detalle del 401: la petición no trae un token válido.</summary>
    internal const string NoAutenticado = "No autenticado: falta el token o no es válido.";

    /// <summary>Detalle del 403: el token es válido, pero su rol no alcanza.</summary>
    internal const string SinPermisos = "Sin permisos para esta operación.";

    private readonly AuthorizationMiddlewareResultHandler _porDefecto = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            await context.ChallengeAsync();
            await EscribirAsync(context, StatusCodes.Status401Unauthorized, NoAutenticado);
        }
        else if (authorizeResult.Forbidden)
        {
            await context.ForbidAsync();
            await EscribirAsync(context, StatusCodes.Status403Forbidden, SinPermisos);
        }
        else
        {
            await _porDefecto.HandleAsync(next, context, policy, authorizeResult);
        }
    }

    private async Task EscribirAsync(HttpContext context, int status, string detalle)
    {
        context.Response.StatusCode = status;
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Detail = detalle },
        });
    }
}
