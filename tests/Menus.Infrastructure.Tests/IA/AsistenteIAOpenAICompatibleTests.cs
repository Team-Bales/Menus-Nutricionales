using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Menus.Application.IA;
using Menus.Infrastructure.IA;
using Microsoft.Extensions.Options;

namespace Menus.Infrastructure.Tests.IA;

/// <summary>Pruebas del adaptador OpenAI-compatible y del asistente deshabilitado (#323).</summary>
public sealed class AsistenteIAOpenAICompatibleTests
{
    private static readonly PromptIA Prompt = new("Eres un asistente nutricional.", "Explica la valoracion.");

    [Fact]
    public async Task Envia_el_prompt_y_devuelve_el_texto()
    {
        var handler = new HandlerFalso((_, _) => Task.FromResult(Respuesta("La valoración es adecuada.")));
        var asistente = Crear(handler);

        var texto = await asistente.CompletarAsync(Prompt);

        var peticion = handler.Peticion!;
        Assert.Equal("La valoración es adecuada.", texto);
        Assert.Equal(HttpMethod.Post, peticion.Method);
        Assert.Equal("http://localhost:11434/v1/chat/completions", peticion.RequestUri!.ToString());
        Assert.Equal("application/json", peticion.Content!.Headers.ContentType!.MediaType);

        using var cuerpo = JsonDocument.Parse(handler.Cuerpo!);
        var raiz = cuerpo.RootElement;
        Assert.Equal("llama3", raiz.GetProperty("model").GetString());
        Assert.Equal(0.2m, raiz.GetProperty("temperature").GetDecimal());

        var mensajes = raiz.GetProperty("messages");
        Assert.Equal(2, mensajes.GetArrayLength());
        Assert.Equal("system", mensajes[0].GetProperty("role").GetString());
        Assert.Equal("Eres un asistente nutricional.", mensajes[0].GetProperty("content").GetString());
        Assert.Equal("user", mensajes[1].GetProperty("role").GetString());
        Assert.Equal("Explica la valoracion.", mensajes[1].GetProperty("content").GetString());
    }

    [Theory]
    [InlineData("http://localhost:11434/v1", "http://localhost:11434/v1/")]
    [InlineData("http://localhost:11434/v1/", "http://localhost:11434/v1/")]
    [InlineData("https://api.openai.com/v1", "https://api.openai.com/v1/")]
    public void La_url_base_termina_en_una_sola_barra(string entrada, string esperada)
    {
        Assert.Equal(esperada, AsistenteIAOpenAICompatible.BaseConBarra(new Uri(entrada)).ToString());
    }

    [Fact]
    public async Task La_clave_solo_viaja_en_la_cabecera_Authorization()
    {
        const string clave = "clave-secreta-123";
        var handler = new HandlerFalso((_, _) => Task.FromResult(Respuesta("ok")));
        var asistente = Crear(handler, apiKey: clave);

        await asistente.CompletarAsync(Prompt);

        var peticion = handler.Peticion!;
        var autorizacion = peticion.Headers.Authorization;
        Assert.NotNull(autorizacion);
        Assert.Equal("Bearer", autorizacion!.Scheme);
        Assert.Equal(clave, autorizacion.Parameter);
        Assert.DoesNotContain(clave, handler.Cuerpo!);
        Assert.DoesNotContain(clave, peticion.RequestUri!.ToString());
        foreach (var cabecera in peticion.Headers.Where(h => h.Key != "Authorization"))
        {
            Assert.DoesNotContain(clave, string.Join(",", cabecera.Value));
        }
    }

    [Fact]
    public async Task Sin_clave_no_envia_la_cabecera_Authorization()
    {
        var handler = new HandlerFalso((_, _) => Task.FromResult(Respuesta("ok")));
        var asistente = Crear(handler);

        await asistente.CompletarAsync(Prompt);

        Assert.Null(handler.Peticion!.Headers.Authorization);
    }

    [Fact]
    public async Task Un_5xx_lanza_AsistenteIANoDisponible()
    {
        var handler = new HandlerFalso((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var asistente = Crear(handler);

        var error = await Assert.ThrowsAsync<AsistenteIANoDisponibleException>(
            () => asistente.CompletarAsync(Prompt));

        Assert.Contains("500", error.Message);
        Assert.DoesNotContain("Eres un asistente nutricional.", error.Message);
    }

    [Fact]
    public async Task Un_error_de_conexion_lanza_AsistenteIANoDisponible()
    {
        var handler = new HandlerFalso((_, _) => throw new HttpRequestException("sin conexión"));
        var asistente = Crear(handler);

        var error = await Assert.ThrowsAsync<AsistenteIANoDisponibleException>(
            () => asistente.CompletarAsync(Prompt));

        Assert.Contains("No se pudo contactar", error.Message);
        Assert.DoesNotContain("Explica la valoracion.", error.Message);
    }

    [Theory]
    [InlineData("{\"choices\":[]}")]
    [InlineData("{\"error\":{\"message\":\"sin cuota\"}}")]
    [InlineData("{\"choices\":{}}")]
    [InlineData("{\"choices\":[{}]}")]
    [InlineData("{\"choices\":[{\"message\":{}}]}")]
    [InlineData("{\"choices\":[{\"message\":{\"content\":null}}]}")]
    [InlineData("{\"choices\":[{\"message\":{\"content\":123}}]}")]
    [InlineData("{\"choices\":[{\"message\":{\"content\":\"\"}}]}")]
    [InlineData("{\"choices\":[{\"message\":{\"content\":\"   \"}}]}")]
    [InlineData("[]")]
    [InlineData("\"texto\"")]
    [InlineData("null")]
    [InlineData("{\"choices\":[\"x\"]}")]
    [InlineData("{\"choices\":[{\"message\":\"x\"}]}")]
    public async Task Una_respuesta_vacia_lanza_AsistenteIANoDisponible(string json)
    {
        var handler = new HandlerFalso((_, _) => Task.FromResult(RespuestaCruda(json)));
        var asistente = Crear(handler);

        await Assert.ThrowsAsync<AsistenteIANoDisponibleException>(() => asistente.CompletarAsync(Prompt));
    }

    [Fact]
    public async Task Una_respuesta_que_no_es_json_lanza_AsistenteIANoDisponible()
    {
        var handler = new HandlerFalso((_, _) => Task.FromResult(RespuestaCruda("esto no es json")));
        var asistente = Crear(handler);

        await Assert.ThrowsAsync<AsistenteIANoDisponibleException>(() => asistente.CompletarAsync(Prompt));
    }

    [Fact]
    public async Task Un_timeout_lanza_AsistenteIANoDisponible()
    {
        var handler = new HandlerFalso(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return Respuesta("tarde");
        });
        var asistente = Crear(handler, timeoutSegundos: 1);

        await Assert.ThrowsAsync<AsistenteIANoDisponibleException>(() => asistente.CompletarAsync(Prompt));
    }

    [Fact]
    public async Task La_cancelacion_del_llamador_se_propaga()
    {
        var handler = new HandlerFalso((_, ct) => Task.FromCanceled<HttpResponseMessage>(ct));
        var asistente = Crear(handler);
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => asistente.CompletarAsync(Prompt, cancelacion.Token));
    }

    [Fact]
    public async Task El_adaptador_con_el_flag_apagado_no_llama_al_proveedor()
    {
        var handler = new HandlerFalso((_, _) => Task.FromResult(Respuesta("no debería llegar")));
        var asistente = Crear(handler, habilitada: false);

        await Assert.ThrowsAsync<AsistenteIANoDisponibleException>(() => asistente.CompletarAsync(Prompt));
        Assert.Null(handler.Peticion);
    }

    [Fact]
    public async Task Un_prompt_nulo_lanza_ArgumentNullException()
    {
        var handler = new HandlerFalso((_, _) => Task.FromResult(Respuesta("no debería llegar")));
        var asistente = Crear(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(() => asistente.CompletarAsync(null!));
        Assert.Null(handler.Peticion);
    }

    [Fact]
    public async Task La_IA_deshabilitada_lanza_AsistenteIANoDisponible()
    {
        var asistente = new AsistenteIADeshabilitado();

        var error = await Assert.ThrowsAsync<AsistenteIANoDisponibleException>(
            () => asistente.CompletarAsync(Prompt));

        Assert.Contains("deshabilitada", error.Message);
    }

    [Fact]
    public async Task La_IA_deshabilitada_rechaza_un_prompt_nulo()
    {
        var asistente = new AsistenteIADeshabilitado();

        await Assert.ThrowsAsync<ArgumentNullException>(() => asistente.CompletarAsync(null!));
    }

    private static AsistenteIAOpenAICompatible Crear(
        HttpMessageHandler handler,
        bool habilitada = true,
        string apiKey = "",
        int timeoutSegundos = 30,
        string model = "llama3")
    {
        var opciones = new IAOptions
        {
            Habilitada = habilitada,
            BaseUrl = "http://localhost:11434/v1",
            Model = model,
            ApiKey = apiKey,
            TimeoutSegundos = timeoutSegundos,
        };

        var http = new HttpClient(handler)
        {
            BaseAddress = AsistenteIAOpenAICompatible.BaseConBarra(new Uri(opciones.BaseUrl)),
            Timeout = TimeSpan.FromSeconds(timeoutSegundos),
        };

        return new AsistenteIAOpenAICompatible(http, Options.Create(opciones));
    }

    private static HttpResponseMessage Respuesta(string contenido)
        => new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { choices = new[] { new { message = new { content = contenido } } } }),
        };

    private static HttpResponseMessage RespuestaCruda(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private sealed class HandlerFalso(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        : HttpMessageHandler
    {
        public HttpRequestMessage? Peticion { get; private set; }

        public string? Cuerpo { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Peticion = request;
            Cuerpo = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return await responder(request, cancellationToken);
        }
    }
}
