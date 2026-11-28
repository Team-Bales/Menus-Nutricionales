using System.Net;
using System.Text;
using System.Text.Json;
using Menus.Client.AsistenteIA;
using Menus.Client.Services;

namespace Menus.Client.Tests.Services;

/// <summary>Pruebas del adaptador HTTP del contrato de consultas de IA (#332).</summary>
public sealed class AsistenteIAApiTests
{
    [Fact]
    public async Task ConsultarAsync_envia_la_solicitud_al_endpoint_con_los_nombres_del_contrato()
    {
        var valoracionId = Guid.NewGuid();
        string? cuerpo = null;
        var handler = new HandlerFalso(request =>
        {
            cuerpo = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json("""{"resumen":"Resumen","secciones":[{"titulo":"Hallazgo","texto":"Texto"}],"recomendaciones":["Revisar"],"advertencias":[],"fuentesUsadas":["valoracion"],"generadoPorIA":true}""");
        });
        var api = new AsistenteIAApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var respuesta = await api.ConsultarAsync(new SolicitudDeIA(
            TareaDeIA.ExplicarAnalisis,
            valoracionId,
            ["valoracion", "aceptacion"],
            "¿Qué cambió?"));

        Assert.Equal(HttpMethod.Post, handler.Recibida!.Method);
        Assert.Equal("/api/ia/consultas", handler.Recibida.RequestUri!.AbsolutePath);
        Assert.Contains("\"tarea\":\"ExplicarAnalisis\"", cuerpo);
        Assert.Contains(valoracionId.ToString(), cuerpo);
        Assert.Contains("\"bloques\":[\"valoracion\",\"aceptacion\"]", cuerpo);
        using var solicitudJson = JsonDocument.Parse(cuerpo!);
        Assert.Equal("¿Qué cambió?", solicitudJson.RootElement.GetProperty("pregunta").GetString());
        Assert.Equal("Resumen", respuesta.Resumen);
        Assert.Equal("Hallazgo", Assert.Single(respuesta.Secciones).Titulo);
        Assert.True(respuesta.GeneradoPorIA);
    }

    [Fact]
    public async Task ConsultarAsync_con_503_expone_el_detail_de_problem_details()
    {
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("""{"detail":"La IA no está disponible ahora."}""", Encoding.UTF8, "application/problem+json"),
        });
        var api = new AsistenteIAApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var error = await Assert.ThrowsAsync<ApiException>(() => api.ConsultarAsync(new SolicitudDeIA(
            TareaDeIA.SugerirAcciones,
            Guid.NewGuid(),
            [],
            null)));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode);
        Assert.Equal("La IA no está disponible ahora.", error.Message);
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class HandlerFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? Recibida { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Recibida = request;
            return Task.FromResult(responder(request));
        }
    }
}
