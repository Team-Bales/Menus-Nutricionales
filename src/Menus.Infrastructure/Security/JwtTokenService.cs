using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Menus.Infrastructure.Security;

/// <summary>Genera tokens JWT firmados con HS256 para los actores del sistema.</summary>
public sealed class JwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    /// <param name="options">Configuración del token.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="options"/> es nulo.</exception>
    /// <exception cref="InvalidOperationException">
    /// Si la clave o el emisor están vacíos, la clave es demasiado corta o la vigencia no es positiva.
    /// </exception>
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        JwtBearerConfiguration.Validate(_options);
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);
    }

    /// <summary>Genera un token JWT con los claims del usuario indicado.</summary>
    /// <param name="userId">Identificador del usuario; se emite en el claim <c>sub</c>.</param>
    /// <param name="role">Rol del usuario: <c>Admin</c>, <c>Nutricionista</c> o <c>Paciente</c>.</param>
    /// <param name="especialidad">Especialidad del nutricionista, si la tiene.</param>
    /// <param name="esJefeNutricion">
    /// Indica si el nutricionista ocupa el cargo de jefe de nutrición. Solo puede ser
    /// <see langword="true"/> cuando <paramref name="role"/> es <c>Nutricionista</c>.
    /// </param>
    /// <returns>El token JWT serializado.</returns>
    /// <exception cref="ArgumentException">
    /// Si <paramref name="role"/> no es un rol válido, o si <paramref name="esJefeNutricion"/> es
    /// <see langword="true"/> para un rol distinto de <c>Nutricionista</c>.
    /// </exception>
    public string GenerateToken(
        Guid userId,
        string role,
        string? especialidad = null,
        bool esJefeNutricion = false)
    {
        if (!JwtRoles.IsValid(role))
        {
            throw new ArgumentException($"El rol '{role}' no es válido.", nameof(role));
        }

        if (esJefeNutricion && role != JwtRoles.Nutricionista)
        {
            throw new ArgumentException(
                "Solo un nutricionista puede ser jefe de nutrición.",
                nameof(esJefeNutricion));
        }

        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Subject, userId.ToString()),
            new(JwtClaimTypes.Role, role),
        };

        if (!string.IsNullOrWhiteSpace(especialidad))
        {
            claims.Add(new Claim(JwtClaimTypes.Especialidad, especialidad));
        }

        if (esJefeNutricion)
        {
            claims.Add(new Claim(JwtClaimTypes.JefeNutricion, bool.TrueString));
        }

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_options.ExpiresMinutes),
            signingCredentials: _credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
