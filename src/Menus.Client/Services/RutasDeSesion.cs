using Microsoft.AspNetCore.Components;

namespace Menus.Client.Services;

/// <summary>Rutas de inicio de sesión y validación del destino al que se vuelve después.</summary>
public static class RutasDeSesion
{
    /// <summary>Ruta de la pantalla de inicio de sesión.</summary>
    public const string Login = "/login";

    /// <summary>
    /// Ruta de login que recuerda la página actual en <c>returnUrl</c>, para volver a ella al entrar.
    /// Si ya se está en el login (o en la raíz), devuelve el login sin parámetro.
    /// </summary>
    /// <param name="navegacion">Navegador de la aplicación, del que se toma la URL actual.</param>
    public static string LoginVolviendoAqui(NavigationManager navegacion)
    {
        var actual = "/" + navegacion.ToBaseRelativePath(navegacion.Uri);
        if (actual == "/" || EsLaPantallaDeLogin(actual))
        {
            return Login;
        }

        return $"{Login}?returnUrl={Uri.EscapeDataString(actual)}";
    }

    /// <summary>
    /// Devuelve <paramref name="returnUrl"/> solo si es una ruta local de la aplicación; si no, la raíz.
    /// Evita redirecciones abiertas a otros dominios (<c>https://…</c>, <c>//dominio</c>, <c>/\dominio</c>)
    /// y volver al propio login.
    /// </summary>
    /// <param name="returnUrl">Destino recibido en la query string.</param>
    public static string DestinoSeguro(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)
            || !returnUrl.StartsWith('/')
            || returnUrl.StartsWith("//", StringComparison.Ordinal)
            || returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            || EsLaPantallaDeLogin(returnUrl))
        {
            return "/";
        }

        return returnUrl;
    }

    private static bool EsLaPantallaDeLogin(string ruta)
        => ruta.Equals(Login, StringComparison.OrdinalIgnoreCase)
           || ruta.StartsWith(Login + "?", StringComparison.OrdinalIgnoreCase)
           || ruta.StartsWith(Login + "/", StringComparison.OrdinalIgnoreCase);
}
