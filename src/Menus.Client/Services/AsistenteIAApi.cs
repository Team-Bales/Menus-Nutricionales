using System.Net.Http.Json;
using System.Text.Json;
using Menus.Client.AsistenteIA;

namespace Menus.Client.Services;

/// <summary>Adaptador HTTP del contrato congelado para las consultas de IA del nutricionista.</summary>
public sealed class AsistenteIAApi(HttpClient http) : IAsistenteIAApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<RespuestaDeIA> ConsultarAsync(SolicitudDeIA solicitud, CancellationToken ct = default)
    {
        var cuerpo = new
        {
            Tarea = solicitud.Tarea.ToString(),
            solicitud.ValoracionId,
            solicitud.Bloques,
            solicitud.Pregunta,
        };
        using var response = await http.PostAsJsonAsync("api/ia/consultas", cuerpo, Json, ct);
        await RespuestasDeApi.LanzarSiErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<RespuestaDeIA>(Json, ct)
            ?? throw new ApiException(response.StatusCode, "La API devolvió una respuesta vacía.");
    }
}
