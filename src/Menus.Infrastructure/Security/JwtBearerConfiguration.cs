using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Menus.Infrastructure.Security;

/// <summary>Configura la validación de los tokens JWT emitidos por <see cref="JwtTokenService"/>.</summary>
public static class JwtBearerConfiguration
{
    /// <summary>Crea los parámetros con los que se valida un token emitido por este sistema.</summary>
    /// <param name="options">Configuración del token.</param>
    /// <returns>Los parámetros de validación.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="options"/> es nulo.</exception>
    /// <exception cref="InvalidOperationException">
    /// Si la clave o el emisor están vacíos, la clave es demasiado corta o la vigencia no es positiva.
    /// </exception>
    public static TokenValidationParameters CreateTokenValidationParameters(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Validate(options);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    }

    /// <summary>Comprueba que la configuración del token sea utilizable.</summary>
    /// <param name="options">Configuración del token.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="options"/> es nulo.</exception>
    /// <exception cref="InvalidOperationException">
    /// Si la clave o el emisor están vacíos, la clave es demasiado corta o la vigencia no es positiva.
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

        if (options.ExpiresMinutes <= 0)
        {
            throw new InvalidOperationException("La vigencia del token JWT (Jwt:ExpiresMinutes) debe ser positiva.");
        }
    }
}
