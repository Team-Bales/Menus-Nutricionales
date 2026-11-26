using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Menus.Client.GestionDeMenus;
using Menus.Client.Services;

namespace Menus.Client.Tests.Services;

/// <summary>Pruebas de <see cref="MenusApi"/> contra un <see cref="HttpMessageHandler"/> falso (#81).</summary>
public sealed class MenusApiTests
{
    private static readonly Guid Dieta = Guid.NewGuid();
    private static readonly Guid TipoUno = Guid.NewGuid();
    private static readonly Guid TipoDos = Guid.NewGuid();
    private static readonly Guid Grupo = Guid.NewGuid();
    private static readonly Guid Menu = Guid.NewGuid();

    private static MenusApi Crear(HandlerFalso handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

    [Fact]
    public async Task GenerarAutomaticamenteAsync_envia_los_parametros_a_la_ruta_de_la_dieta()
    {
        var creado = Guid.NewGuid();
        string? cuerpo = null;
        var handler = new HandlerFalso(req =>
        {
            cuerpo = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json(req, $$"""{"id":"{{creado}}"}""");
        });
        var solicitud = new SolicitudDeGeneracion(
            Dieta,
            12,
            [new(TipoUno, 0.25m), new(TipoDos, 0.75m)],
            [Grupo]);

        var id = await Crear(handler).GenerarAutomaticamenteAsync(solicitud);

        Assert.Equal(creado, id);
        Assert.Equal(HttpMethod.Post, handler.Recibida!.Method);
        Assert.Equal($"/api/dietas/{Dieta}/menus/generacion-automatica", handler.Recibida.RequestUri!.AbsolutePath);
        using var json = JsonDocument.Parse(cuerpo!);
        Assert.Equal(12, json.RootElement.GetProperty("cantidadTotalAlimentos").GetInt32());
        var distribucion = json.RootElement.GetProperty("distribucion");
        Assert.Equal(2, distribucion.GetArrayLength());
        Assert.Equal(0.25m, distribucion[0].GetProperty("proporcion").GetDecimal());
        Assert.Equal(Grupo.ToString(), json.RootElement.GetProperty("gruposNutricionalesIds")[0].GetString());
    }

    [Fact]
    public async Task ListarPorDietaAsync_arma_la_ruta_y_deserializa_el_sobre_paginado()
    {
        var revisor = Guid.NewGuid();
        var reemplaza = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req,
            $$"""{"data":[{"id":"{{Menu}}","dietaId":"{{Dieta}}","fechaCreacion":"2026-01-15T10:30:00Z","creador":{"id":"{{Guid.NewGuid()}}","nombre":"Ana Pérez"},"esAutomatico":true,"cantidadTotalAlimentos":12,"estado":"Final","dictamen":{"revisor":{"id":"{{revisor}}","nombre":"Jefa"},"fecha":"2026-01-16T10:30:00Z","aprobado":true,"observaciones":null},"reemplazaA":"{{reemplaza}}"}],"total":1,"page":2,"size":5}"""));

        var pagina = await Crear(handler).ListarPorDietaAsync(Dieta, 2, 5);

        Assert.Equal("/api/menus", handler.Recibida!.RequestUri!.AbsolutePath);
        Assert.Contains($"dietaId={Dieta}", handler.Recibida.RequestUri.Query);
        Assert.Contains("page=2", handler.Recibida.RequestUri.Query);
        Assert.Contains("size=5", handler.Recibida.RequestUri.Query);
        var menu = Assert.Single(pagina.Data);
        Assert.Equal(Menu, menu.Id);
        Assert.Equal(Dieta, menu.DietaId);
        Assert.Equal(new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc), menu.FechaCreacion);
        Assert.Equal("Ana Pérez", menu.Creador.Nombre);
        Assert.True(menu.EsAutomatico);
        Assert.Equal(12, menu.CantidadTotalAlimentos);
        Assert.Equal("Final", menu.Estado);
        Assert.Equal("Jefa", menu.Dictamen!.Revisor.Nombre);
        Assert.Equal(revisor, menu.Dictamen.Revisor.Id);
        Assert.True(menu.Dictamen.Aprobado);
        Assert.Null(menu.Dictamen.Observaciones);
        Assert.Equal(reemplaza, menu.ReemplazaA);
        Assert.Equal(1, pagina.Total);
    }

    [Fact]
    public async Task ObtenerAsync_pide_la_ruta_del_menu_y_deserializa_los_parametros()
    {
        var handler = new HandlerFalso(req => Json(req,
            $$"""{"id":"{{Menu}}","dietaId":"{{Dieta}}","fechaCreacion":"2026-01-15T10:30:00Z","creador":{"id":"{{Guid.NewGuid()}}","nombre":"Ana Pérez"},"esAutomatico":true,"cantidadTotalAlimentos":12,"estado":"Pendiente","dictamen":null,"reemplazaA":null,"distribucion":[{"tipoPreparacionId":"{{TipoUno}}","nombre":"Plato fuerte","proporcion":1.0}],"gruposNutricionales":[{"id":"{{Grupo}}","nombre":"Cereales"}],"items":[{"id":"{{Guid.NewGuid()}}","nombre":"Avena"}]}"""));

        var detalle = await Crear(handler).ObtenerAsync(Menu);

        Assert.Equal($"/api/menus/{Menu}", handler.Recibida!.RequestUri!.AbsolutePath);
        Assert.Equal(Menu, detalle.Id);
        Assert.Equal(Dieta, detalle.DietaId);
        Assert.Equal(new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc), detalle.FechaCreacion);
        Assert.Equal("Ana Pérez", detalle.Creador.Nombre);
        Assert.True(detalle.EsAutomatico);
        Assert.Equal(12, detalle.CantidadTotalAlimentos);
        Assert.Equal("Pendiente", detalle.Estado);
        Assert.Null(detalle.Dictamen);
        Assert.Null(detalle.ReemplazaA);
        Assert.Equal("Plato fuerte", Assert.Single(detalle.Distribucion).Nombre);
        Assert.Equal(TipoUno, detalle.Distribucion[0].TipoPreparacionId);
        Assert.Equal(1m, detalle.Distribucion[0].Proporcion);
        Assert.Equal("Cereales", Assert.Single(detalle.GruposNutricionales).Nombre);
        Assert.Equal(Grupo, detalle.GruposNutricionales[0].Id);
        Assert.Equal("Avena", Assert.Single(detalle.Items).Nombre);
    }

    [Fact]
    public async Task Un_error_de_la_api_lanza_ApiException_con_el_detail()
    {
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("""{"detail":"La dieta no tiene grupos configurados."}""", Encoding.UTF8, "application/problem+json")
        });

        var error = await Assert.ThrowsAsync<ApiException>(() => Crear(handler).ObtenerAsync(Menu));

        Assert.Equal(HttpStatusCode.Conflict, error.StatusCode);
        Assert.Equal("La dieta no tiene grupos configurados.", error.Message);
    }

    [Fact]
    public async Task ObtenerAsync_lanza_si_la_respuesta_exitosa_viene_vacia()
    {
        var handler = new HandlerFalso(_ => Vacia(HttpStatusCode.OK));

        var error = await Assert.ThrowsAsync<ApiException>(() => Crear(handler).ObtenerAsync(Menu));

        Assert.Equal("La API devolvió una respuesta vacía.", error.Message);
    }

    [Fact]
    public async Task GenerarAutomaticamenteAsync_lanza_si_la_respuesta_exitosa_viene_vacia()
    {
        var handler = new HandlerFalso(_ => Vacia(HttpStatusCode.Created));

        var error = await Assert.ThrowsAsync<ApiException>(() => Crear(handler).GenerarAutomaticamenteAsync(
            new SolicitudDeGeneracion(Dieta, 12, [new(TipoUno, 1m)], [Grupo])));

        Assert.Equal("La API devolvió una respuesta vacía.", error.Message);
    }

    private static HttpResponseMessage Json(HttpRequestMessage _, string json)
        => new(HttpStatusCode.OK) { Content = JsonContent.Create(JsonDocument.Parse(json).RootElement) };

    private static HttpResponseMessage Vacia(HttpStatusCode estado)
        => new(estado) { Content = new StringContent("null", Encoding.UTF8, "application/json") };

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
