using System.Security.Claims;
using System.Text;
using Menus.Domain.Abstractions.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Menus.Infrastructure.Security;

/// <summary>Configura la validación de los tokens JWT emitidos por <see cref="JwtTokenService"/>.</summary>
public static class JwtBearerConfiguration
{
    /// <summary>Crea los parámetros con los que se valida un token emitido por este sistema.</summary>
    /// <param name="options">Configuración del token.</param>
    /// <returns>Los parámetros de validación.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="options"/> es nulo.</exception>
    /// <exception cref="InvalidOperationException">Si la configuración del token no es válida (vea <see cref="Validate"/>).</exception>
    public static TokenValidationParameters CreateTokenValidationParameters(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Validate(options);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds),
            RoleClaimType = JwtClaimTypes.Role,
            NameClaimType = JwtClaimTypes.Subject,
        };
    }

    /// <summary>
    /// Eventos que rechazan los tokens sin los claims obligatorios, aunque la firma y la vigencia sean correctas.
    /// Es lo que convierte un token incompleto en un 401.
    /// </summary>
    public static JwtBearerEvents CreateJwtBearerEvents() => new()
    {
        OnTokenValidated = context =>
        {
            if (!TieneClaimsObligatorios(context.Principal))
            {
                context.Fail("El token JWT no incluye los claims obligatorios sub y role.");
            }

            return Task.CompletedTask;
        },
    };

    /// <summary>Comprueba que el principal lleve identificador de usuario y rol.</summary>
    public static bool TieneClaimsObligatorios(ClaimsPrincipal? principal) =>
        principal is not null &&
        !string.IsNullOrWhiteSpace(principal.FindFirst(JwtClaimTypes.Subject)?.Value) &&
        !string.IsNullOrWhiteSpace(principal.FindFirst(JwtClaimTypes.Role)?.Value);

    /// <summary>Comprueba que la configuración del token sea utilizable.</summary>
    /// <param name="options">Configuración del token.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="options"/> es nulo.</exception>
    /// <exception cref="InvalidOperationException">
    /// Si la clave, el emisor o la audiencia están vacíos, la clave es demasiado corta, la vigencia está fuera de
    /// rango o el desfase de reloj es negativo o excesivo.
    /// </exception>
    public static void Validate(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Key))
        {
            throw new InvalidOperationException(
                "La clave JWT (Jwt:Key) no está configurada. Defínala en una variable de entorno o en user secrets.");
        }

        if (Encoding.UTF8.GetByteCount(options.Key) < JwtOptions.MinimumKeyLengthBytes)
        {
            throw new InvalidOperationException(
                $"La clave JWT (Jwt:Key) debe tener al menos {JwtOptions.MinimumKeyLengthBytes} bytes para firmar con HS256.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("El emisor JWT (Jwt:Issuer) no está configurado.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("La audiencia JWT (Jwt:Audience) no está configurada.");
        }

        if (options.ExpiresMinutes <= 0 || options.ExpiresMinutes > JwtOptions.MaxExpiresMinutes)
        {
            throw new InvalidOperationException(
                $"La vigencia del token JWT (Jwt:ExpiresMinutes) debe estar entre 1 y {JwtOptions.MaxExpiresMinutes} minutos.");
        }

        if (options.ClockSkewSeconds < 0 || options.ClockSkewSeconds > JwtOptions.MaxClockSkewSeconds)
        {
            throw new InvalidOperationException(
                $"El desfase permitido (Jwt:ClockSkewSeconds) debe estar entre 0 y {JwtOptions.MaxClockSkewSeconds} segundos.");
        }
    }
}
