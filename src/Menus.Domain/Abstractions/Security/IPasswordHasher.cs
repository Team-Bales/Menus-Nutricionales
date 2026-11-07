namespace Menus.Domain.Abstractions.Security;

/// <summary>
/// Contrato para el hash y la verificación de contraseñas.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Genera un hash BCrypt a partir de una contraseña en texto plano.
    /// </summary>
    /// <param name="password">Contraseña en texto plano.</param>
    /// <returns>Hash BCrypt seguro.</returns>
    /// <exception cref="ArgumentException">Si <paramref name="password"/> es nulo o vacío.</exception>
    string Hash(string password);

    /// <summary>
    /// Verifica si una contraseña en texto plano coincide con su hash BCrypt almacenado.
    /// </summary>
    /// <param name="password">Contraseña en texto plano intentada.</param>
    /// <param name="passwordHash">Hash BCrypt almacenado.</param>
    /// <returns><see langword="true"/> si la contraseña coincide; <see langword="false"/> en caso contrario.</returns>
    /// <exception cref="ArgumentException">Si <paramref name="password"/> o <paramref name="passwordHash"/> son nulos o vacíos.</exception>
    bool Verify(string password, string passwordHash);

    /// <summary>
    /// Ejecuta una verificación "dummy" contra un hash BCrypt válido generado una sola vez por proceso.
    /// Se usa para evitar ataques de temporización cuando el usuario no existe: el tiempo de respuesta
    /// es indistinguible de una verificación real fallida.
    /// </summary>
    /// <param name="attemptedPassword">Contraseña intentada por el cliente.</param>
    void VerifyDummy(string attemptedPassword);
}