namespace Menus.Application.IA;

/// <summary>
/// Excepción que indica que el asistente de IA no está disponible.
/// Se lanza cuando el proveedor está caído, hay timeout, la respuesta está vacía,
/// la IA está deshabilitada o el prompt fue abortado por el auditor.
/// </summary>
/// <remarks>
/// Sigue el patrón de <c>ServicioNoDisponibleException</c>. No acepta ni conserva
/// el prompt, la respuesta ni la ApiKey.
/// </remarks>
public sealed class AsistenteIANoDisponibleException : Exception
{
    /// <summary>
    /// Inicializa una nueva instancia con el mensaje de error.
    /// </summary>
    /// <param name="mensaje">Mensaje que describe la causa de la indisponibilidad. No puede ser nulo.</param>
    /// <exception cref="ArgumentNullException"><paramref name="mensaje"/> es nulo.</exception>
    public AsistenteIANoDisponibleException(string mensaje)
        : base(mensaje ?? throw new ArgumentNullException(nameof(mensaje)))
    {
    }

    /// <summary>
    /// Inicializa una nueva instancia con el mensaje de error y la excepción causa.
    /// </summary>
    /// <param name="mensaje">Mensaje que describe la causa de la indisponibilidad. No puede ser nulo.</param>
    /// <param name="causa">Excepción que provocó esta excepción. No puede ser nula.</param>
    /// <exception cref="ArgumentNullException"><paramref name="mensaje"/> o <paramref name="causa"/> es nulo.</exception>
    public AsistenteIANoDisponibleException(string mensaje, Exception causa)
        : base(mensaje ?? throw new ArgumentNullException(nameof(mensaje)), causa ?? throw new ArgumentNullException(nameof(causa)))
    {
    }
}
