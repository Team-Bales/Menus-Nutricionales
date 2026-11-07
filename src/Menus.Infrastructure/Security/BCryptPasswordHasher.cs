using BCryptNet = BCrypt.Net.BCrypt;
using HashType = BCrypt.Net.HashType;
using SaltParseException = BCrypt.Net.SaltParseException;
using Menus.Domain.Abstractions.Security;

namespace Menus.Infrastructure.Security;

/// <summary>
/// Implementación de <see cref="IPasswordHasher"/> usando BCrypt.Net-Next.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private static readonly Lazy<string> DummyHash = new(() =>
    {
        // Genera un hash BCrypt válido una sola vez por proceso con una contraseña aleatoria
        // que nunca se registrará. Se usa para VerifyDummy y evitar ataques de temporización.
        var randomPassword = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        return BCryptNet.HashPassword(randomPassword, workFactor: 12);
    }, isThreadSafe: true);

    /// <inheritdoc/>
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        // workFactor 12 es el default recomendado para BCrypt en 2024+
        // Usamos BCrypt estándar (sin enhanced entropy) para simplicidad y compatibilidad
        return BCryptNet.HashPassword(password, workFactor: 12);
    }

    /// <inheritdoc/>
    public bool Verify(string password, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        try
        {
            // BCrypt estándar: enhancedEntropy = false, hashType = None
            return BCryptNet.Verify(password, passwordHash, enhancedEntropy: false, hashType: HashType.None);
        }
        catch (SaltParseException)
        {
            // Hash malformado: tratamos como verificación fallida sin revelar detalles
            return false;
        }
    }

    /// <inheritdoc/>
    public void VerifyDummy(string attemptedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attemptedPassword);
        // Ejecuta una verificación contra el hash dummy para igualar el tiempo de respuesta
        // cuando el usuario no existe. No loguea, no lanza, solo consume CPU equivalente.
        _ = BCryptNet.Verify(attemptedPassword, DummyHash.Value, enhancedEntropy: false, hashType: HashType.None);
    }
}