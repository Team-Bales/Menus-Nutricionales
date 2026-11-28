using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Menus.Client.GestionMenus;
using Menus.Client.Services;

namespace Menus.Client.Tests.Services;

/// <summary>Pruebas del contrato HTTP de revisión de menús (#82).</summary>
public sealed class MenusApiTests
{
    private static MenusApi Crear(HandlerFalso handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

    [Fact]
    public async Task ObtenerAsync_pide_el_detalle_y_deserializa_el_menu()
    {
        var menuId = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req, $$"""{"id":"{{menuId}}","dietaId":"{{Guid.NewGuid()}}","fechaCreacion":"2026-11-28T12:00:00Z","creador":{"id":"{{Guid.NewGuid()}}","nombre":"Ana Pérez"},"esAutomatico":true,"cantidadTotalAlimentos":6,"estado":"Pendiente","dictamen":null,"reemplazaA":null,"distribucion":[],"gruposNutricionales":[],"items":[]}"""));

        var menu = await Crear(handler).ObtenerAsync(menuId);

        Assert.Equal($"/api/menus/{menuId}", handler.Recibida!.RequestUri!.AbsolutePath);
        Assert.Equal("Ana Pérez", menu.Creador.Nombre);
        Assert.Equal("Pendiente", menu.Estado);
    }

    [Fact]
    public async Task ValidarAsync_envia_el_dictamen_a_la_ruta_correcta()
    {
        var menuId = Guid.NewGuid();
        string? cuerpo = null;
        var handler = new HandlerFalso(req =>
        {
            cuerpo = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        await Crear(handler).ValidarAsync(menuId, aprobado: false, new DictamenFormulario { Observaciones = "Ajustar la distribución." });

        Assert.Equal(HttpMethod.Post, handler.Recibida!.Method);
        Assert.Equal($"/api/menus/{menuId}/validar", handler.Recibida.RequestUri!.AbsolutePath);
        using var json = JsonDocument.Parse(cuerpo!);
        Assert.False(json.RootElement.GetProperty("aprobado").GetBoolean());
        Assert.Equal("Ajustar la distribución.", json.RootElement.GetProperty("observaciones").GetString());
    }

    private static HttpResponseMessage Json(HttpRequestMessage _, string json)
        => new(HttpStatusCode.OK) { Content = JsonContent.Create(JsonDocument.Parse(json).RootElement) };

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
