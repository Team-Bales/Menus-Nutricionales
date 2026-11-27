using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Menus.Client.MenusGenerados;
using Menus.Client.Services;

namespace Menus.Client.Tests.Services;

/// <summary>Pruebas del contrato HTTP de <see cref="MenusApi"/> (issue #81).</summary>
public sealed class MenusApiTests
{
    private static MenusApi Crear(HandlerFalso handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

    [Fact]
    public async Task ListarMenusAsync_arma_la_ruta_con_la_dieta_la_pagina_y_el_estado()
    {
        var dieta = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req, """{"data":[],"total":0,"page":2,"size":5}"""));

        var pagina = await Crear(handler).ListarMenusAsync(dieta, "Final", 2, 5);

        Assert.Equal("/api/menus", handler.Recibida!.RequestUri!.AbsolutePath);
        Assert.Contains($"dietaId={dieta}", handler.Recibida.RequestUri.Query);
        Assert.Contains("estado=Final", handler.Recibida.RequestUri.Query);
        Assert.Contains("page=2", handler.Recibida.RequestUri.Query);
        Assert.Contains("size=5", handler.Recibida.RequestUri.Query);
        Assert.Equal(2, pagina.Page);
    }

    [Fact]
    public async Task ListarMenusAsync_omite_el_estado_cuando_no_se_indica()
    {
        var handler = new HandlerFalso(req => Json(req, """{"data":[],"total":0,"page":1,"size":10}"""));

        await Crear(handler).ListarMenusAsync(Guid.NewGuid(), null, 1, 10);

        Assert.DoesNotContain("estado=", handler.Recibida!.RequestUri!.Query);
    }

    [Fact]
    public async Task ObtenerMenuAsync_deserializa_la_parametrizacion_y_los_nombres()
    {
        var id = Guid.NewGuid();
        var dieta = Guid.NewGuid();
        var creador = Guid.NewGuid();
        var tipo = Guid.NewGuid();
        var grupo = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req, $$"""
            {
              "id": "{{id}}",
              "dietaId": "{{dieta}}",
              "fechaCreacion": "2026-01-15T12:00:00Z",
              "creador": { "id": "{{creador}}", "nombre": "Ana Pérez" },
              "esAutomatico": true,
              "cantidadTotalAlimentos": 10,
              "estado": "Pendiente",
              "dictamen": null,
              "reemplazaA": null,
              "distribucion": [ { "tipoPreparacionId": "{{tipo}}", "nombre": "Plato fuerte", "proporcion": 1 } ],
              "gruposNutricionales": [ { "id": "{{grupo}}", "nombre": "Cereales" } ],
              "items": []
            }
            """));

        var menu = await Crear(handler).ObtenerMenuAsync(id);

        Assert.Equal($"/api/menus/{id}", handler.Recibida!.RequestUri!.AbsolutePath);
        Assert.Equal("Ana Pérez", menu.Creador.Nombre);
        Assert.Equal([new ProporcionDelMenu(tipo, "Plato fuerte", 1m)], menu.Distribucion);
        Assert.Equal([new ElementoDelMenu(grupo, "Cereales")], menu.GruposNutricionales);
    }

    [Fact]
    public async Task GenerarMenuAsync_envia_post_con_los_parametros_y_devuelve_el_id()
    {
        var creado = Guid.NewGuid();
        var dieta = Guid.NewGuid();
        var tipo = Guid.NewGuid();
        var grupo = Guid.NewGuid();
        string? cuerpo = null;
        var handler = new HandlerFalso(req =>
        {
            cuerpo = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json(req, $$"""{"id":"{{creado}}"}""");
        });

        var id = await Crear(handler).GenerarMenuAsync(new(
            dieta,
            12,
            [new(tipo, 0.6m), new(Guid.NewGuid(), 0.4m)],
            [grupo]));

        Assert.Equal(HttpMethod.Post, handler.Recibida!.Method);
        Assert.Equal($"/api/dietas/{dieta}/menus/generacion-automatica", handler.Recibida.RequestUri!.AbsolutePath);
        Assert.Equal(creado, id);
        Assert.Contains("\"cantidadTotalAlimentos\":12", cuerpo);
        Assert.Contains($"\"tipoPreparacionId\":\"{tipo}\"", cuerpo);
        Assert.Contains($"\"gruposNutricionalesIds\":[\"{grupo}\"]", cuerpo);
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
