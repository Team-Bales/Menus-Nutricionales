namespace Menus.Application.IA;

/// <summary>
/// Puerto público del asistente de IA. Solo transporte: recibe un <see cref="PromptIA"/> ya construido y devuelve texto.
/// No contiene lógica de proveedor, caché, privacidad ni auditoría.
/// </summary>
/// <remarks>
/// Las implementaciones deben lanzar <see cref="AsistenteIANoDisponibleException"/> cuando el proveedor esté caído,
/// haya timeout, la respuesta esté vacía, la IA esté deshabilitada o el prompt haya sido abortado por el auditor.
/// </remarks>
public interface IAsistenteIA
{
    /// <summary>
    /// Completa el prompt y devuelve la respuesta del modelo como texto plano.
    /// </summary>
    /// <param name="prompt">Prompt ya construido con rol de sistema y rol de usuario. No puede ser nulo.</param>
    /// <param name="ct">Token de cancelación para abortar la llamada al proveedor.</param>
    /// <returns>Texto devuelto por el modelo. Nunca nulo en caso de éxito.</returns>
    /// <exception cref="AsistenteIANoDisponibleException">
    /// Proveedor caído, timeout, respuesta vacía, IA deshabilitada o prompt abortado por el auditor.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="prompt"/> es nulo.</exception>
    Task<string> CompletarAsync(PromptIA prompt, CancellationToken ct = default);
}





