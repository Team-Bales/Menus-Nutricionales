using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Menus.Client.Administracion;
using Menus.Client.Services;

namespace Menus.Client.Tests.Services;

/// <summary>Pruebas del contrato HTTP de <see cref="AdministracionApi"/> (issue #76).</summary>
public sealed class AdministracionApiTests
{
    private static AdministracionApi Crear(HandlerFalso handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

    [Fact]
    public async Task ListarNutricionistasAsync_usa_la_ruta_paginada_y_el_filtro_de_especialidad()
    {
        var especialidad = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req, """{"data":[],"total":0,"page":2,"size":5}"""));

        await Crear(handler).ListarNutricionistasAsync(especialidad, 2, 5);

        Assert.Equal("/api/nutricionistas", handler.Recibida!.RequestUri!.AbsolutePath);
        Assert.Contains($"especialidadId={especialidad}", handler.Recibida.RequestUri.Query);
        Assert.Contains("page=2", handler.Recibida.RequestUri.Query);
        Assert.Contains("size=5", handler.Recibida.RequestUri.Query);
    }

    [Fact]
    public async Task ListarNutricionistasAsync_omite_el_filtro_cuando_no_se_indica_especialidad()
    {
        var handler = new HandlerFalso(req => Json(req, """{"data":[],"total":0,"page":1,"size":10}"""));

        await Crear(handler).ListarNutricionistasAsync(null, 1, 10);

        Assert.DoesNotContain("especialidadId=", handler.Recibida!.RequestUri!.Query);
    }

    [Fact]
    public async Task ObtenerNutricionistaAsync_deserializa_sus_dietas_autorizadas()
    {
        var nutricionista = Guid.NewGuid();
        var dieta = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req, $$"""{"id":"{{nutricionista}}","nombre":"Ana","especialidadId":"{{Guid.Empty}}","esJefe":false,"dietasAutorizadas":[{"dietaId":"{{dieta}}"}]}"""));

        var detalle = await Crear(handler).ObtenerNutricionistaAsync(nutricionista);

        Assert.Equal($"/api/nutricionistas/{nutricionista}", handler.Recibida!.RequestUri!.AbsolutePath);
        Assert.Equal(dieta, Assert.Single(detalle.DietasAutorizadas).DietaId);
    }

    [Fact]
    public async Task Operaciones_de_autorizacion_y_jefatura_envian_las_rutas_correctas()
    {
        var rutas = new List<(HttpMethod Metodo, string Ruta)>();
        var handler = new HandlerFalso(req =>
        {
            rutas.Add((req.Method, req.RequestUri!.AbsolutePath));
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var nutricionista = Guid.NewGuid();
        var dieta = Guid.NewGuid();
        var api = Crear(handler);

        await api.AutorizarDietaAsync(nutricionista, dieta);
        await api.RevocarDietaAsync(nutricionista, dieta);
        await api.AsignarRolJefeAsync(nutricionista);

        Assert.Equal(
            [
                (HttpMethod.Post, $"/api/nutricionistas/{nutricionista}/dietas"),
                (HttpMethod.Delete, $"/api/nutricionistas/{nutricionista}/dietas/{dieta}"),
                (HttpMethod.Post, $"/api/nutricionistas/{nutricionista}/rol-jefe"),
            ],
            rutas);
    }

    [Fact]
    public async Task Salas_usan_las_rutas_y_cuerpos_correctos()
    {
        var solicitudes = new List<HttpRequestMessage>();
        var sala = Guid.NewGuid();
        var handler = new HandlerFalso(req =>
        {
            solicitudes.Add(req);
            return req.Method == HttpMethod.Post ? Json(req, $$"""{"id":"{{sala}}"}""") : new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var api = Crear(handler);

        await api.CrearSalaAsync(new SalaFormulario { Nombre = "Sala 101" });
        await api.ActualizarSalaAsync(sala, new SalaFormulario { Nombre = "Sala A" });
        await api.EliminarSalaAsync(sala);

        Assert.Equal(HttpMethod.Post, solicitudes[0].Method);
        Assert.Equal("/api/salas", solicitudes[0].RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Put, solicitudes[1].Method);
        Assert.Equal($"/api/salas/{sala}", solicitudes[1].RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Delete, solicitudes[2].Method);
        Assert.Equal($"/api/salas/{sala}", solicitudes[2].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ContarPacientesEnSalaAsync_lee_el_total_de_la_pagina_filtrada()
    {
        var sala = Guid.NewGuid();
        var paciente = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req, $$"""{"data":[{"id":"{{paciente}}"}],"total":3,"page":1,"size":1}"""));

        var cantidad = await Crear(handler).ContarPacientesEnSalaAsync(sala);

        Assert.Equal(3, cantidad);
        Assert.Equal("/api/pacientes", handler.Recibida!.RequestUri!.AbsolutePath);
        Assert.Contains($"sala={sala}", handler.Recibida.RequestUri.Query);
        Assert.Contains("size=1", handler.Recibida.RequestUri.Query);
    }

    [Fact]
    public async Task ListarSalasAsync_pide_la_ruta_y_deserializa_la_lista()
    {
        var sala = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req, $$"""[{"id":"{{sala}}","nombre":"Sala 101"}]"""));

        var salas = await Crear(handler).ListarSalasAsync();

        Assert.Equal("/api/salas", handler.Recibida!.RequestUri!.AbsolutePath);
        Assert.Equal(new Sala(sala, "Sala 101"), Assert.Single(salas));
    }

    [Fact]
    public async Task Error_de_la_api_conserva_el_detail_del_problem_details()
    {
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(new { detail = "La sala tiene pacientes asignados." }),
        });

        var error = await Assert.ThrowsAsync<ApiException>(() => Crear(handler).EliminarSalaAsync(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Conflict, error.StatusCode);
        Assert.Equal("La sala tiene pacientes asignados.", error.Message);
    }

    [Fact]
    public async Task Respuestas_exitosas_vacias_se_rechazan_en_consultas_y_creaciones()
    {
        var respuestas = new Queue<HttpResponseMessage>(
        [
            JsonNulo(),
            JsonNulo(),
        ]);
        var handler = new HandlerFalso(_ => respuestas.Dequeue());
        var api = Crear(handler);

        await Assert.ThrowsAsync<ApiException>(() => api.ObtenerNutricionistaAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<ApiException>(() => api.CrearSalaAsync(new SalaFormulario { Nombre = "Sala 101" }));
    }

    private static HttpResponseMessage Json(HttpRequestMessage _, string json)
        => new(HttpStatusCode.OK) { Content = JsonContent.Create(JsonDocument.Parse(json).RootElement) };

    private static HttpResponseMessage JsonNulo()
        => new(HttpStatusCode.OK) { Content = JsonContent.Create<object?>(null) };

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
