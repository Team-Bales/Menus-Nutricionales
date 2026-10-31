namespace Menus.Infrastructure.Security;

/// <summary>
/// Configuración del token JWT, leída de la sección <c>Jwt</c> de la configuración de la aplicación.
/// </summary>
/// <remarks>
/// <see cref="Key"/> nunca se versiona: se toma de variables de entorno o de user secrets
/// (<c>Jwt__Key</c> en el entorno). El repositorio solo declara la sección con la clave vacía.
/// </remarks>
public sealed class JwtOptions
{
    /// <summary>Nombre de la sección de configuración.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Longitud mínima, en bytes, que debe tener <see cref="Key"/> para firmar con HS256.</summary>
    public const int MinimumKeyLengthBytes = 32;

    /// <summary>Clave simétrica usada para firmar y validar el token.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Emisor del token.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Minutos de vigencia del token.</summary>
    public int ExpiresMinutes { get; set; } = 60;
}
