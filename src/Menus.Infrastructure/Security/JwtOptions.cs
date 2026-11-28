namespace Menus.Infrastructure.Security;

/// <summary>
/// Configuración del token JWT, leída de la sección <c>Jwt</c> de la configuración de la aplicación.
/// </summary>
/// <remarks>
/// <see cref="Key"/> nunca se versiona: se toma de variables de entorno o de user secrets
/// (<c>Jwt__Key</c> en el entorno). El repositorio solo declara la sección con la clave vacía.
/// <see cref="Issuer"/> y <see cref="Audience"/> también se configuran por entorno y se validan al arrancar.
/// </remarks>
public sealed class JwtOptions
{
    /// <summary>Nombre de la sección de configuración.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Longitud mínima, en bytes, que debe tener <see cref="Key"/> para firmar con HS256.</summary>
    public const int MinimumKeyLengthBytes = 32;

    /// <summary>Vigencia corta por defecto: sin refresh token, se prefiere reautenticar antes que alargar la sesión.</summary>
    public const int DefaultExpiresMinutes = 15;

    /// <summary>Vigencia máxima admitida, para evitar configuraciones que dejen tokens válidos demasiado tiempo.</summary>
    public const int MaxExpiresMinutes = 60;

    /// <summary>Desfase de reloj permitido por defecto al validar <c>exp</c>/<c>nbf</c>.</summary>
    public const int DefaultClockSkewSeconds = 30;

    /// <summary>Desfase de reloj máximo admitido.</summary>
    public const int MaxClockSkewSeconds = 60;

    /// <summary>Clave simétrica usada para firmar y validar el token.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Emisor del token.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Audiencia del token: solo se aceptan tokens emitidos para este destino.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Minutos de vigencia del token.</summary>
    public int ExpiresMinutes { get; set; } = DefaultExpiresMinutes;

    /// <summary>Segundos de desfase de reloj permitidos al validar la vigencia.</summary>
    public int ClockSkewSeconds { get; set; } = DefaultClockSkewSeconds;
}
