using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Menus.Application.IA;
using Microsoft.Extensions.Options;

namespace Menus.Infrastructure.IA;

/// <summary>
/// Adaptador de <see cref="IAsistenteIA"/> para cualquier proveedor con la interfaz OpenAI
/// (<c>/v1/chat/completions</c>): Ollama local, OpenAI o NVIDIA NIM (#323). Es solo transporte: envía el
/// <see cref="PromptIA"/> tal cual y devuelve el texto del modelo.
/// </summary>
/// <remarks>
/// La <see cref="IAOptions.ApiKey"/> viaja únicamente en la cabecera <c>Authorization</c>; nunca se incluye en el
/// prompt, en el cuerpo de la petición ni en los mensajes de error. El adaptador no registra el prompt ni la
/// respuesta: no recibe ningún registro de eventos.
/// </remarks>
/// <param name="http">
/// Cliente HTTP que configura el contenedor de dependencias con la <c>BaseAddress</c> y el timeout de
/// <see cref="IAOptions"/>.
/// </param>
/// <param name="opciones">Opciones de la integración de IA: modelo, clave y estado del feature flag.</param>
public sealed class AsistenteIAOpenAICompatible(HttpClient http, IOptions<IAOptions> opciones) : IAsistenteIA
{
    /// <summary>Temperatura baja: el asistente explica datos ya calculados, no improvisa.</summary>
    private static readonly decimal Temperatura = 0.2m;

    /// <summary>
    /// Devuelve la URL base con una barra final para que la ruta relativa <c>chat/completions</c> conserve el
    /// segmento del proveedor (<c>/v1</c> en Ollama, OpenAI y NVIDIA NIM).
    /// </summary>
    /// <param name="baseUrl">URL base ya validada como absoluta http o https.</param>
    /// <returns>La misma URL con una sola barra final.</returns>
    internal static Uri BaseConBarra(Uri baseUrl) => new(baseUrl.AbsoluteUri.TrimEnd('/') + "/");

    /// <inheritdoc/>
    public async Task<string> CompletarAsync(PromptIA prompt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        var configuracion = opciones.Value;
        if (!configuracion.Habilitada)
        {
            throw new AsistenteIANoDisponibleException("La integración de IA está deshabilitada.");
        }

        using var peticion = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(new
            {
                model = configuracion.Model,
                messages = new[]
                {
                    new { role = "system", content = prompt.Sistema },
                    new { role = "user", content = prompt.Usuario },
                },
                temperature = Temperatura,
            }),
        };

        if (!string.IsNullOrWhiteSpace(configuracion.ApiKey))
        {
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuracion.ApiKey);
        }

        try
        {
            using var respuesta = await http.SendAsync(peticion, ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                throw new AsistenteIANoDisponibleException(
                    $"El proveedor de IA respondió con el estado {(int)respuesta.StatusCode}.");
            }

            var json = await respuesta.Content.ReadAsStringAsync(ct);
            var texto = LeerContenido(json);
            if (string.IsNullOrWhiteSpace(texto))
            {
                throw new AsistenteIANoDisponibleException("El proveedor de IA devolvió una respuesta vacía.");
            }

            return texto;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // HttpClient convierte el vencimiento de su Timeout en TaskCanceledException. La cancelación del
            // llamador no es un fallo del proveedor: se propaga.
            throw new AsistenteIANoDisponibleException("El proveedor de IA no respondió a tiempo.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new AsistenteIANoDisponibleException("No se pudo contactar con el proveedor de IA.", ex);
        }
        catch (JsonException ex)
        {
            throw new AsistenteIANoDisponibleException(
                "El proveedor de IA devolvió una respuesta que no se pudo interpretar.", ex);
        }
    }

    /// <summary>Lee <c>choices[0].message.content</c> sin lanzar si falta o no es una cadena.</summary>
    private static string? LeerContenido(string json)
    {
        using var documento = JsonDocument.Parse(json);
        var raiz = documento.RootElement;
        if (!raiz.TryGetProperty("choices", out var elecciones)
            || elecciones.ValueKind != JsonValueKind.Array
            || elecciones.GetArrayLength() == 0)
        {
            return null;
        }

        var primera = elecciones[0];
        if (!primera.TryGetProperty("message", out var mensaje)
            || !mensaje.TryGetProperty("content", out var contenido)
            || contenido.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return contenido.GetString();
    }
}
