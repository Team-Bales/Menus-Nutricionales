using Menus.Domain;
using Microsoft.AspNetCore.Authorization;

namespace Menus.Api.Auth;

/// <summary>
/// Políticas de autorización de la API (#74), por rol del token (<see cref="RolesUsuario"/>).
/// </summary>
/// <remarks>
/// <para>
/// Por defecto, todo endpoint exige un usuario autenticado, de cualquier rol: es la política de reserva
/// (<c>FallbackPolicy</c>), así que un endpoint nuevo no queda abierto por olvido. Solo el login es anónimo
/// (<c>[AllowAnonymous]</c>). Los endpoints que piden un rol lo declaran con <c>[Authorize(Policy = ...)]</c>.
/// </para>
/// <para>
/// Las reglas que dependen de los datos, y no solo del rol, siguen en los casos de uso: que quien dictamina un menú
/// sea jefe de nutrición autorizado para la dieta (RS-03), por ejemplo.
/// </para>
/// </remarks>
public static class Politicas
{
    /// <summary>Solo el administrador: dietas, nomencladores y registro de usuarios.</summary>
    public const string SoloAdmin = "SoloAdmin";

    /// <summary>
    /// Solo un nutricionista: lo que hace como actor (<c>IActorActual</c>), como describir un alimento o un plato,
    /// dictaminar un menú o valorarlo. El administrador no tiene un nutricionista asociado en el token.
    /// </summary>
    public const string SoloNutricionista = "SoloNutricionista";

    /// <summary>Un nutricionista o el administrador: la gestión de pacientes.</summary>
    public const string NutricionistaOAdmin = "NutricionistaOAdmin";

    /// <summary>
    /// Registra las políticas, la de reserva y la respuesta en <c>ProblemDetails</c> de los 401 y 403
    /// (<see cref="RespuestaDeAutorizacion"/>).
    /// </summary>
    /// <param name="services">Contenedor de servicios de la API.</param>
    public static IServiceCollection AddPoliticasDeAutorizacion(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(SoloAdmin, p => p.RequireRole(RolesUsuario.Admin))
            .AddPolicy(SoloNutricionista, p => p.RequireRole(RolesUsuario.Nutricionista))
            .AddPolicy(NutricionistaOAdmin, p => p.RequireRole(RolesUsuario.Nutricionista, RolesUsuario.Admin));

        return services.AddSingleton<IAuthorizationMiddlewareResultHandler, RespuestaDeAutorizacion>();
    }
}
