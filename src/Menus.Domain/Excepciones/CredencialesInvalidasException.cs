namespace Menus.Domain.Excepciones;

/// <summary>
/// Las credenciales de acceso son inválidas: email inexistente o contraseña incorrecta.
/// Se usa para responder 401 uniforme sin revelar qué campo falló.
/// </summary>
public sealed class CredencialesInvalidasException : ExcepcionDeDominio
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CredencialesInvalidasException"/>.
    /// </summary>
    /// <param name="mensaje">Mensaje genérico para el usuario (p. ej. "Credenciales inválidas").</param>
    public CredencialesInvalidasException(string mensaje = "Credenciales inválidas.")
        : base(mensaje, regla: null)
    {
    }
}
