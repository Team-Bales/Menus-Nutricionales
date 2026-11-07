using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Menus.Domain.Abstractions.Security;
using Menus.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Menus.Infrastructure.Security;

/// <summary>
/// Implementación de <see cref="ITokenEmitter"/> que genera tokens JWT firmados con HS256.
/// </summary>
public sealed class JwtTokenService : ITokenEmitter
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

    /// <inheritdoc/>
    public TokenEmitido Emitir(
        Guid usuarioId,
        string rol,
        Guid? entidadId = null,
        string? especialidad = null,
        bool esJefeNutricion = false)
    {
        if (!RolesUsuario.EsValido(rol))
        {
            throw new ArgumentException($"El rol '{rol}' no es válido.", nameof(rol));
        }

        if (esJefeNutricion && rol != RolesUsuario.Nutricionista)
        {
            throw new ArgumentException(
                "Solo un nutricionista puede ser jefe de nutrición.",
                nameof(esJefeNutricion));
        }

        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Subject, usuarioId.ToString()),
            new(JwtClaimTypes.Role, rol),
        };

        if (entidadId.HasValue)
        {
            claims.Add(new Claim(JwtClaimTypes.EntidadId, entidadId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(especialidad))
        {
            claims.Add(new Claim(JwtClaimTypes.Especialidad, especialidad));
        }

        if (esJefeNutricion)
        {
            claims.Add(new Claim(JwtClaimTypes.JefeNutricion, bool.TrueString));
        }

        var ahora = DateTime.UtcNow;
        var expira = ahora.AddMinutes(_options.ExpiresMinutes);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            claims: claims,
            notBefore: ahora,
            expires: expira,
            signingCredentials: _credentials);

        var tokenSerializado = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenEmitido(tokenSerializado, expira);
    }

    /// <summary>
    /// Genera un token JWT con los claims del usuario indicado (método legacy para compatibilidad).
    /// </summary>
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
    [Obsolete("Use Emitir(...) que devuelve TokenEmitido con expiración. Este método se mantendrá por compatibilidad temporal.")]
    public string GenerateToken(
        Guid userId,
        string role,
        string? especialidad = null,
        bool esJefeNutricion = false)
        => Emitir(userId, role, null, especialidad, esJefeNutricion).Token;
}