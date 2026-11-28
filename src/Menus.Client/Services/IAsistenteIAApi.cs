using Menus.Client.AsistenteIA;

namespace Menus.Client.Services;

/// <summary>Puerto del cliente para consultar la asistencia de IA desde una valoración.</summary>
public interface IAsistenteIAApi
{
    /// <summary>Solicita una explicación, recomendación o guion para el caso seleccionado.</summary>
    /// <param name="solicitud">Tarea, valoración, bloques y pregunta opcional elegidos por el nutricionista.</param>
    /// <param name="ct">Token para cancelar la consulta HTTP.</param>
    /// <returns>La respuesta verificada por la API, con su trazabilidad.</returns>
    Task<RespuestaDeIA> ConsultarAsync(SolicitudDeIA solicitud, CancellationToken ct = default);
}
