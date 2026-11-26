namespace Menus.Application.Comun;

/// <summary>
/// Un servicio externo del que depende la operación no está disponible: no está configurado o no responde. La API la
/// traduce a 503.
/// </summary>
/// <remarks>
/// Hoy la usa la ficha nutricional (#283): MongoDB es opcional, y sin él la API arranca igual, pero no puede leer ni
/// escribir fichas.
/// </remarks>
public sealed class ServicioNoDisponibleException : Exception
{
    /// <summary>Crea la excepción con el mensaje para el cliente.</summary>
    /// <param name="mensaje">Qué no está disponible, sin detalles internos.</param>
    public ServicioNoDisponibleException(string mensaje)
        : base(mensaje)
    {
    }

    /// <summary>Crea la excepción con el mensaje para el cliente y la causa.</summary>
    /// <param name="mensaje">Qué no está disponible, sin detalles internos.</param>
    /// <param name="causa">La excepción del servicio, que queda en el registro y no llega al cliente.</param>
    public ServicioNoDisponibleException(string mensaje, Exception causa)
        : base(mensaje, causa)
    {
    }
}
