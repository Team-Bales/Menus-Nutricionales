namespace Menus.Client.Services;

/// <summary>Mensajes que se muestran cuando la API no llega a responder.</summary>
public static class MensajesDeConexion
{
    /// <summary>La petición no obtuvo respuesta: API caída, sin red, CORS o tiempo agotado.</summary>
    public const string SinServidor = "No se pudo conectar con el servidor. Comprueba tu conexión e inténtalo de nuevo.";
}
