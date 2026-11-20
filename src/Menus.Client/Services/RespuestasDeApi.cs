using System.Text.Json;

namespace Menus.Client.Services;

/// <summary>Traducción común de las respuestas de error de la API a <see cref="ApiException"/>.</summary>
internal static class RespuestasDeApi
{
    /// <summary>
    /// No hace nada si la respuesta es exitosa; si no, lanza <see cref="ApiException"/> con el
    /// <c>detail</c> del <c>ProblemDetails</c> o, si no lo hay, con un mensaje genérico.
    /// </summary>
    /// <param name="response">Respuesta recibida de la API.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <exception cref="ApiException">La respuesta no tiene un código de éxito.</exception>
    public static async Task LanzarSiErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var mensaje = $"La API respondió {(int)response.StatusCode}.";
        var contenido = await response.Content.ReadAsStringAsync(ct);
        if (!string.IsNullOrWhiteSpace(contenido))
        {
            try
            {
                using var documento = JsonDocument.Parse(contenido);
                if (documento.RootElement.ValueKind == JsonValueKind.Object
                    && documento.RootElement.TryGetProperty("detail", out var detalle)
                    && detalle.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(detalle.GetString()))
                {
                    mensaje = detalle.GetString()!;
                }
            }
            catch (JsonException)
            {
                // La respuesta no usa ProblemDetails; se conserva un mensaje genérico y seguro.
            }
        }

        throw new ApiException(response.StatusCode, mensaje);
    }
}
