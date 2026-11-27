namespace Menus.Application.IA;

/// <summary>
/// Prompt inmutable que se envía al asistente de IA. Contiene el rol de sistema y el rol de usuario.
/// Su constructor es <c>internal</c> para que solo lo pueda crear el <c>ConstructorDePrompt</c> (issue #326).
/// Un ensamblado externo no puede construirlo ni cambiar sus textos mediante <c>with</c>.
/// </summary>
/// <remarks>
/// No guarda el prompt original, claves ni metadatos adicionales. Su <see cref="ToString"/> nunca incluye
/// el contenido de <see cref="Sistema"/> ni <see cref="Usuario"/> por seguridad.
/// </remarks>
public sealed record PromptIA
{
    /// <summary>
    /// Texto del rol de sistema. No nulo.
    /// </summary>
    public string Sistema { get; }

    /// <summary>
    /// Texto del rol de usuario. No nulo.
    /// </summary>
    public string Usuario { get; }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PromptIA"/>.
    /// </summary>
    /// <param name="sistema">Texto del rol de sistema. No puede ser nulo.</param>
    /// <param name="usuario">Texto del rol de usuario. No puede ser nulo.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sistema"/> o <paramref name="usuario"/> es nulo.</exception>
    internal PromptIA(string sistema, string usuario)
    {
        ArgumentNullException.ThrowIfNull(sistema);
        ArgumentNullException.ThrowIfNull(usuario);

        Sistema = sistema;
        Usuario = usuario;
    }

    /// <summary>
    /// Devuelve una representación textual que no expone el contenido del prompt.
    /// </summary>
    /// <returns>Descripción genérica sin el contenido de <see cref="Sistema"/> ni <see cref="Usuario"/>.</returns>
    public override string ToString() => "PromptIA { Sistema = [redacted], Usuario = [redacted] }";
}
