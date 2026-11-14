using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Menus.Client.Catalogo;
using Menus.Client.Services;

namespace Menus.Client.Tests.Services;

/// <summary>Pruebas de <see cref="CatalogoApi"/> contra un <see cref="HttpMessageHandler"/> falso (issue #65).</summary>
public sealed class CatalogoApiTests
{
    private static CatalogoApi Crear(HandlerFalso handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

    [Fact]
    public async Task ListarGruposAsync_pide_la_ruta_de_nomencladores_y_deserializa_la_lista()
    {
        var id = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req, """[{"id":"$ID","nombre":"Cereales"}]""".Replace("$ID", id.ToString())));

        var grupos = await Crear(handler).ListarGruposAsync();

        Assert.Equal(HttpMethod.Get, handler.Recibida!.Method);
        Assert.Equal("/api/nomencladores/grupos-nutricionales", handler.Recibida.RequestUri!.AbsolutePath);
        Assert.Equal([new Nomenclador(id, "Cereales")], grupos);
    }

    [Fact]
    public async Task ListarTiposPreparacionAsync_pide_la_ruta_de_nomencladores()
    {
        var handler = new HandlerFalso(req => Json(req, "[]"));

        await Crear(handler).ListarTiposPreparacionAsync();

        Assert.Equal("/api/nomencladores/tipos-preparacion", handler.Recibida!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ListarNivelesCaloricosAsync_pide_la_ruta_de_nomencladores()
    {
        var handler = new HandlerFalso(req => Json(req, "[]"));

        await Crear(handler).ListarNivelesCaloricosAsync();

        Assert.Equal("/api/nomencladores/niveles-caloricos", handler.Recibida!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ListarRestriccionesAsync_pide_la_ruta_de_nomencladores()
    {
        var handler = new HandlerFalso(req => Json(req, "[]"));

        await Crear(handler).ListarRestriccionesAsync();

        Assert.Equal("/api/nomencladores/restricciones-alimentarias", handler.Recibida!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ListarProgramas_y_especialidades_piden_sus_rutas_de_nomencladores()
    {
        var rutas = new List<string>();
        var handler = new HandlerFalso(req =>
        {
            rutas.Add(req.RequestUri!.AbsolutePath);
            return Json(req, "[]");
        });
        var api = Crear(handler);

        await api.ListarProgramasAsync();
        await api.ListarEspecialidadesAsync();

        Assert.Equal(
            ["/api/nomencladores/programas-atencion", "/api/nomencladores/especialidades"],
            rutas);
    }

    [Fact]
    public async Task CrearNomencladorAsync_envia_post_a_la_ruta_del_tipo_elegido()
    {
        var creado = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req, $$"""{"id":"{{creado}}"}"""));

        var id = await Crear(handler).CrearNomencladorAsync(
            TipoDeNomenclador.ProgramasAtencion,
            new NomencladorFormulario { Nombre = "Atención primaria" });

        Assert.Equal(HttpMethod.Post, handler.Recibida!.Method);
        Assert.Equal("/api/nomencladores/programas-atencion", handler.Recibida.RequestUri!.AbsolutePath);
        Assert.Equal(creado, id);
    }

    [Fact]
    public async Task EliminarNomencladorAsync_envia_delete_a_la_ruta_del_elemento()
    {
        var id = Guid.NewGuid();
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Crear(handler).EliminarNomencladorAsync(TipoDeNomenclador.Especialidades, id);

        Assert.Equal(HttpMethod.Delete, handler.Recibida!.Method);
        Assert.Equal($"/api/nomencladores/especialidades/{id}", handler.Recibida.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ListarDietasAsync_arma_la_ruta_paginada_con_el_programa()
    {
        var programa = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req, """{"data":[],"total":0,"page":2,"size":5}"""));

        await Crear(handler).ListarDietasAsync(programa, 2, 5);

        var query = handler.Recibida!.RequestUri!.Query;
        Assert.Equal("/api/dietas", handler.Recibida.RequestUri.AbsolutePath);
        Assert.Contains($"programa={programa}", query);
        Assert.Contains("page=2", query);
        Assert.Contains("size=5", query);
    }

    [Fact]
    public async Task ObtenerDietaAsync_pide_la_ruta_y_deserializa_las_relaciones()
    {
        var dietaId = Guid.NewGuid();
        var grupoId = Guid.NewGuid();
        var restriccionId = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req,
            $$"""{"id":"{{dietaId}}","nombre":"Basal","programaAtencionId":"{{Guid.Empty}}","grupos":[{"grupoNutricionalId":"{{grupoId}}","tipo":1}],"restricciones":["{{restriccionId}}"]}"""));

        var dieta = await Crear(handler).ObtenerDietaAsync(dietaId);

        Assert.Equal($"/api/dietas/{dietaId}", handler.Recibida!.RequestUri!.AbsolutePath);
        Assert.Equal("Basal", dieta.Nombre);
        Assert.Equal(new GrupoCubierto(grupoId, TipoCobertura.Restringido), Assert.Single(dieta.Grupos));
        Assert.Equal([restriccionId], dieta.Restricciones);
    }

    [Fact]
    public async Task AgregarGrupoDietaAsync_envia_el_tipo_de_cobertura_a_la_ruta_de_la_dieta()
    {
        var dietaId = Guid.NewGuid();
        var grupoId = Guid.NewGuid();
        string? cuerpo = null;
        var handler = new HandlerFalso(req =>
        {
            cuerpo = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        await Crear(handler).AgregarGrupoDietaAsync(dietaId, new GrupoCubiertoFormulario
        {
            GrupoNutricionalId = grupoId,
            Tipo = TipoCobertura.Restringido,
        });

        Assert.Equal(HttpMethod.Post, handler.Recibida!.Method);
        Assert.Equal($"/api/dietas/{dietaId}/grupos", handler.Recibida.RequestUri!.AbsolutePath);
        Assert.Contains(grupoId.ToString(), cuerpo);
        Assert.Contains("1", cuerpo);
    }

    [Fact]
    public async Task AgregarRestriccionDietaAsync_envia_post_a_la_ruta_de_la_dieta()
    {
        var dietaId = Guid.NewGuid();
        var restriccionId = Guid.NewGuid();
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Crear(handler).AgregarRestriccionDietaAsync(dietaId, restriccionId);

        Assert.Equal(HttpMethod.Post, handler.Recibida!.Method);
        Assert.Equal($"/api/dietas/{dietaId}/restricciones", handler.Recibida.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Quitar_relaciones_de_dieta_envia_delete_a_las_rutas_correctas()
    {
        var rutas = new List<string>();
        var handler = new HandlerFalso(req =>
        {
            rutas.Add(req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var dietaId = Guid.NewGuid();
        var grupoId = Guid.NewGuid();
        var restriccionId = Guid.NewGuid();
        var api = Crear(handler);

        await api.QuitarGrupoDietaAsync(dietaId, grupoId);
        await api.QuitarRestriccionDietaAsync(dietaId, restriccionId);

        Assert.Equal(
            [$"/api/dietas/{dietaId}/grupos/{grupoId}", $"/api/dietas/{dietaId}/restricciones/{restriccionId}"],
            rutas);
    }

    [Fact]
    public async Task ListarAlimentosAsync_arma_la_ruta_paginada_con_los_filtros_presentes()
    {
        var handler = new HandlerFalso(req => Json(req, """{"data":[],"total":0,"page":2,"size":5}"""));
        var grupo = Guid.NewGuid();
        var nivel = Guid.NewGuid();

        await Crear(handler).ListarAlimentosAsync(grupo, null, nivel, 2, 5);

        var query = handler.Recibida!.RequestUri!.Query;
        Assert.Contains("page=2", query);
        Assert.Contains("size=5", query);
        Assert.Contains($"grupo={grupo}", query);
        Assert.Contains($"nivel={nivel}", query);
        Assert.DoesNotContain("tipo=", query);
    }

    [Fact]
    public async Task ListarTodosLosAlimentosAsync_junta_todas_las_paginas()
    {
        var idsPagina1 = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var idsPagina2 = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();
        var peticiones = 0;
        var handler = new HandlerFalso(req =>
        {
            peticiones++;
            var esPrimera = req.RequestUri!.Query.Contains("page=1");
            var data = string.Join(',', (esPrimera ? idsPagina1 : idsPagina2).Select(AlimentoJson));
            return Json(req, $$"""{"data":[{{data}}],"total":110,"page":{{(esPrimera ? 1 : 2)}},"size":100}""");
        });

        var alimentos = await Crear(handler).ListarTodosLosAlimentosAsync();

        Assert.Equal(2, peticiones);
        Assert.Equal(110, alimentos.Count);
        Assert.Equal(idsPagina1.Concat(idsPagina2), alimentos.Select(a => a.Id));
    }

    private static string AlimentoJson(Guid id)
        => $$"""{"id":"{{id}}","nombre":"Avena","grupoNutricionalId":"{{Guid.Empty}}","tipoPreparacionId":"{{Guid.Empty}}","nivelCaloricoId":"{{Guid.Empty}}","nutricionistaId":"{{Guid.Empty}}"}""";

    [Fact]
    public async Task CrearAlimentoAsync_envia_post_con_el_formulario_y_devuelve_el_id()
    {
        var creado = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req, $$"""{"id":"{{creado}}"}"""));
        var formulario = new ClasificacionFormulario { Nombre = "Avena", GrupoNutricionalId = Guid.NewGuid() };

        var id = await Crear(handler).CrearAlimentoAsync(formulario);

        Assert.Equal(HttpMethod.Post, handler.Recibida!.Method);
        Assert.Equal("/api/alimentos", handler.Recibida.RequestUri!.AbsolutePath);
        Assert.Equal(creado, id);
    }

    [Fact]
    public async Task ActualizarAlimentoAsync_envia_put_solo_con_la_clasificacion()
    {
        var id = Guid.NewGuid();
        HttpRequestMessage? recibida = null;
        string? cuerpo = null;
        var handler = new HandlerFalso(req =>
        {
            recibida = req;
            cuerpo = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        await Crear(handler).ActualizarAlimentoAsync(id, new ClasificacionFormulario { Nombre = "Avena" });

        Assert.Equal(HttpMethod.Put, recibida!.Method);
        Assert.Equal($"/api/alimentos/{id}", recibida.RequestUri!.AbsolutePath);
        Assert.DoesNotContain("nombre", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EliminarAlimentoAsync_envia_delete_sin_cuerpo()
    {
        var id = Guid.NewGuid();
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Crear(handler).EliminarAlimentoAsync(id);

        Assert.Equal(HttpMethod.Delete, handler.Recibida!.Method);
        Assert.Null(handler.Recibida.Content);
    }

    [Fact]
    public async Task CrearPlatoAsync_envia_la_clasificacion_y_los_ingredientes()
    {
        var creado = Guid.NewGuid();
        string? cuerpo = null;
        var handler = new HandlerFalso(req =>
        {
            cuerpo = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json(req, $$"""{"id":"{{creado}}"}""");
        });
        var ingredientes = new List<IngredienteFormulario> { new() { AlimentoId = Guid.NewGuid(), Cantidad = 2m } };

        var id = await Crear(handler).CrearPlatoAsync(new ClasificacionFormulario { Nombre = "Avena con leche" }, ingredientes);

        Assert.Equal(creado, id);
        Assert.Contains("ingredientes", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListarPlatosAsync_arma_la_ruta_paginada_con_el_filtro_de_tipo()
    {
        var handler = new HandlerFalso(req => Json(req, """{"data":[],"total":0,"page":1,"size":10}"""));
        var tipo = Guid.NewGuid();

        await Crear(handler).ListarPlatosAsync(null, tipo, null, 1, 10);

        var query = handler.Recibida!.RequestUri!.Query;
        Assert.Contains($"tipo={tipo}", query);
        Assert.DoesNotContain("grupo=", query);
        Assert.DoesNotContain("nivel=", query);
    }

    [Fact]
    public async Task ObtenerPlatoAsync_pide_la_ruta_del_plato()
    {
        var id = Guid.NewGuid();
        var handler = new HandlerFalso(req => Json(req, $$"""{"id":"{{id}}","nombre":"Avena con leche","grupoNutricionalId":"{{Guid.Empty}}","tipoPreparacionId":"{{Guid.Empty}}","nivelCaloricoId":"{{Guid.Empty}}","nutricionistaId":"{{Guid.Empty}}","ingredientes":[]}"""));

        var plato = await Crear(handler).ObtenerPlatoAsync(id);

        Assert.Equal($"/api/platos/{id}", handler.Recibida!.RequestUri!.AbsolutePath);
        Assert.Equal(id, plato.Id);
    }

    [Fact]
    public async Task ActualizarPlatoAsync_envia_put_solo_con_la_clasificacion()
    {
        var id = Guid.NewGuid();
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Crear(handler).ActualizarPlatoAsync(id, new ClasificacionFormulario { Nombre = "Avena con leche" });

        Assert.Equal(HttpMethod.Put, handler.Recibida!.Method);
        Assert.Equal($"/api/platos/{id}", handler.Recibida.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task EliminarPlatoAsync_envia_delete_sin_cuerpo()
    {
        var id = Guid.NewGuid();
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Crear(handler).EliminarPlatoAsync(id);

        Assert.Equal(HttpMethod.Delete, handler.Recibida!.Method);
        Assert.Equal($"/api/platos/{id}", handler.Recibida.RequestUri!.AbsolutePath);
        Assert.Null(handler.Recibida.Content);
    }

    [Fact]
    public async Task AgregarIngredienteAsync_envia_post_a_la_ruta_del_plato()
    {
        var platoId = Guid.NewGuid();
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Crear(handler).AgregarIngredienteAsync(platoId, new IngredienteFormulario { AlimentoId = Guid.NewGuid(), Cantidad = 1m });

        Assert.Equal(HttpMethod.Post, handler.Recibida!.Method);
        Assert.Equal($"/api/platos/{platoId}/ingredientes", handler.Recibida.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task QuitarIngredienteAsync_envia_delete_a_la_ruta_del_alimento()
    {
        var platoId = Guid.NewGuid();
        var alimentoId = Guid.NewGuid();
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Crear(handler).QuitarIngredienteAsync(platoId, alimentoId);

        Assert.Equal(HttpMethod.Delete, handler.Recibida!.Method);
        Assert.Equal($"/api/platos/{platoId}/ingredientes/{alimentoId}", handler.Recibida.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task LanzarSiErrorAsync_usa_el_detail_de_ProblemDetails_cuando_lo_hay()
    {
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("""{"detail":"El alimento está en uso."}""", Encoding.UTF8, "application/problem+json")
        });

        var error = await Assert.ThrowsAsync<ApiException>(() => Crear(handler).EliminarAlimentoAsync(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Conflict, error.StatusCode);
        Assert.Equal("El alimento está en uso.", error.Message);
    }

    [Fact]
    public async Task LanzarSiErrorAsync_usa_un_mensaje_generico_si_el_cuerpo_no_es_ProblemDetails()
    {
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("<html>falla</html>", Encoding.UTF8, "text/html")
        });

        var error = await Assert.ThrowsAsync<ApiException>(() => Crear(handler).EliminarAlimentoAsync(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);
        Assert.Equal("La API respondió 500.", error.Message);
    }

    [Fact]
    public async Task LanzarSiErrorAsync_usa_un_mensaje_generico_si_no_hay_cuerpo()
    {
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var error = await Assert.ThrowsAsync<ApiException>(() => Crear(handler).EliminarAlimentoAsync(Guid.NewGuid()));

        Assert.Equal("La API respondió 404.", error.Message);
    }

    [Fact]
    public async Task ObtenerAsync_lanza_si_la_respuesta_exitosa_viene_vacia()
    {
        var handler = new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null", Encoding.UTF8, "application/json") });

        await Assert.ThrowsAsync<ApiException>(() => Crear(handler).ObtenerAlimentoAsync(Guid.NewGuid()));
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
