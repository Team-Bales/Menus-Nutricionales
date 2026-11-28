namespace Menus.Application.IA;

/// <summary>Reglas de seguridad para habilitar proveedores de IA desde la configuración.</summary>
public static class ValidadorDeConfiguracionIA
{
    /// <summary>Versión del texto de términos que el sistema reconoce como vigente.</summary>
    public const string VersionDeTerminosVigente = "1";

    /// <summary>Indica si la URL identifica un proveedor que corre localmente.</summary>
    /// <param name="baseUrl">URL base configurada para el proveedor.</param>
    /// <returns><see langword="true"/> si la URL apunta a localhost, loopback u Ollama local.</returns>
    public static bool EsProveedorLocal(string? baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.IsLoopback
            || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "ollama", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Determina si la configuración habilita un proveedor que no es local.</summary>
    /// <param name="opciones">Opciones de la integración de IA.</param>
    /// <returns><see langword="true"/> si se habilitó un proveedor externo.</returns>
    public static bool UsaProveedorExterno(IAOptions opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        return opciones.Habilitada && !EsProveedorLocal(opciones.BaseUrl);
    }

    /// <summary>Valida que un proveedor externo tenga la aceptación de términos requerida.</summary>
    /// <param name="opciones">Opciones que se validan al arrancar la API.</param>
    /// <param name="error">Mensaje seguro para mostrar al operador cuando la validación falla.</param>
    /// <returns><see langword="true"/> si se puede continuar el arranque.</returns>
    public static bool TryValidar(IAOptions opciones, out string? error)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        error = null;
        if (!UsaProveedorExterno(opciones))
        {
            return true;
        }

        if (!Uri.TryCreate(opciones.BaseUrl, UriKind.Absolute, out _))
        {
            error = "IA:BaseUrl debe ser una URL absoluta cuando IA:Habilitada=true.";
            return false;
        }

        var terminos = opciones.Terminos ?? new TerminosIAOptions();
        if (!string.Equals(terminos.Version, VersionDeTerminosVigente, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(terminos.AceptadoPor)
            || terminos.Fecha is null
            || !terminos.RetencionDesactivada)
        {
            error = "Un proveedor de IA externo exige IA:Terminos:Version=1, AceptadoPor, Fecha y RetencionDesactivada=true.";
            return false;
        }

        return true;
    }
}
