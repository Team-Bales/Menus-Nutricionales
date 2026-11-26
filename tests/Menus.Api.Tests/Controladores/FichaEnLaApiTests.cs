using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Menus.Application.Catalogo.Fichas;
using Menus.Application.Catalogo.Fichas.GuardarFicha;
using Menus.Application.Catalogo.Fichas.ObtenerFicha;
using Menus.Application.Comun;
using Menus.Domain;
using Menus.Infrastructure.Mongo;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Api.Tests.Controladores;

/// <summary>
/// La ficha nutricional con la API entera en memoria (#283): el JSON del cuerpo llega al comando con sus bloques
/// libres, el validador corre antes del caso de uso, la respuesta sale en camelCase, y sin MongoDB se responde 503.
/// </summary>
/// <remarks>
/// Aquí no hay base: los casos de uso se reemplazan por falsos que guardan lo que reciben. El de guardar usa el
/// <see cref="IAlmacenDeFichas"/> registrado de verdad, que sin MongoDB es <see cref="SinAlmacenDeFichas"/>.
/// </remarks>
public sealed class FichaEnLaApiTests
{
    private const string CuerpoDePlato = """
        {
          "porcionReferencia": { "cantidad": 1, "unidad": "ración (350 g)" },
          "macronutrientes": { "energiaKcal": 486, "proteinasG": 31.5 },
          "micronutrientes": { "minerales": { "sodioMg": 620 } },
          "alergenos": ["apio"],
          "modoPreparacion": { "pasos": [ { "orden": 1, "descripcion": "Dorar." } ] },
          "ingredientes": [ { "alimentoId": "a1100000-0000-4000-8000-000000000015", "aporte": { "energiaKcal": 248 } } ]
        }
        """;

    [Fact]
    public async Task Guardar_lleva_el_cuerpo_al_comando_y_sin_MongoDB_responde_503()
    {
        var plato = Guid.NewGuid();
        var guardar = new GuardarFichaFalso();
        await using var api = Api(s => s.AddScoped<IHandler<GuardarFichaCommand, FichaNutricionalDto>>(
            sp => guardar.Con(sp.GetRequiredService<IAlmacenDeFichas>())));

        using var respuesta = await EnviarAsync(api, HttpMethod.Put, $"/api/platos/{plato}/ficha", CuerpoDePlato);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(SinAlmacenDeFichas.Mensaje, (string?)problema!["detail"]);

        var comando = guardar.Recibido!;
        Assert.Equal(ClaseDeItem.Plato, comando.Clase);
        Assert.Equal(plato, comando.ItemId);
        Assert.Equal(new PorcionDeReferenciaDto(1, "ración (350 g)"), comando.PorcionReferencia);
        Assert.Equal(486, (int)comando.Macronutrientes!["energiaKcal"]!);
        Assert.Equal(620, (int)comando.Micronutrientes!["minerales"]!["sodioMg"]!);
        Assert.Equal(["apio"], comando.Alergenos!);
        Assert.Equal("Dorar.", (string?)comando.ModoPreparacion!["pasos"]![0]!["descripcion"]);
        var ingrediente = Assert.Single(comando.Ingredientes!);
        Assert.Equal(Guid.Parse("a1100000-0000-4000-8000-000000000015"), ingrediente.AlimentoId);
        Assert.Equal(248, (int)ingrediente.Aporte!["energiaKcal"]!);
    }

    [Fact]
    public async Task Un_alergeno_repetido_responde_422_sin_llegar_al_caso_de_uso()
    {
        var guardar = new GuardarFichaFalso();
        await using var api = Api(s => s.AddScoped<IHandler<GuardarFichaCommand, FichaNutricionalDto>>(
            sp => guardar.Con(sp.GetRequiredService<IAlmacenDeFichas>())));

        using var respuesta = await EnviarAsync(
            api, HttpMethod.Put, $"/api/alimentos/{Guid.NewGuid()}/ficha", """{ "alergenos": ["Apio", " ápio "] }""");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Null(guardar.Recibido);
    }

    [Fact]
    public async Task Obtener_devuelve_la_ficha_en_camelCase_con_sus_bloques()
    {
        var alimento = Guid.NewGuid();
        var ficha = new FichaNutricionalDto(
            alimento,
            "Alimento",
            "simple",
            new PorcionDeReferenciaDto(100, "g"),
            new JsonObject { ["energiaKcal"] = 52 },
            null,
            [],
            null,
            null,
            new DateTime(2026, 11, 26, 10, 0, 0, DateTimeKind.Utc));
        await using var api = Api(s => s.AddScoped<IHandler<ObtenerFichaQuery, FichaNutricionalDto>>(
            _ => new ObtenerFichaFalso(ficha)));

        using var respuesta = await EnviarAsync(api, HttpMethod.Get, $"/api/alimentos/{alimento}/ficha", null, RolesUsuario.Paciente);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(alimento, (Guid)json!["itemId"]!);
        Assert.Equal("Alimento", (string?)json["clase"]);
        Assert.Equal("simple", (string?)json["tipo"]);
        Assert.Equal(52, (int)json["macronutrientes"]!["energiaKcal"]!);
        Assert.Empty(json["alergenos"]!.AsArray());
        Assert.Equal(100, (double)json["porcionReferencia"]!["cantidad"]!);
    }

    private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Api(Action<IServiceCollection> servicios)
        => ApiEnMemoria.Crear().WithWebHostBuilder(builder => builder.ConfigureTestServices(servicios));

    private static async Task<HttpResponseMessage> EnviarAsync(
        Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> api,
        HttpMethod metodo,
        string ruta,
        string? cuerpo,
        string rol = RolesUsuario.Nutricionista)
    {
        using var peticion = new HttpRequestMessage(metodo, ruta);
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiEnMemoria.Token(rol, Guid.NewGuid()));
        if (cuerpo is not null)
        {
            peticion.Content = new StringContent(cuerpo, Encoding.UTF8, "application/json");
        }

        return await api.CreateClient().SendAsync(peticion);
    }

    /// <summary>Guarda el comando recibido y lo pasa al almacén registrado, como haría el caso de uso.</summary>
    private sealed class GuardarFichaFalso : IHandler<GuardarFichaCommand, FichaNutricionalDto>
    {
        private IAlmacenDeFichas? _almacen;

        public GuardarFichaCommand? Recibido { get; private set; }

        public GuardarFichaFalso Con(IAlmacenDeFichas almacen)
        {
            _almacen = almacen;
            return this;
        }

        public async Task<FichaNutricionalDto> HandleAsync(GuardarFichaCommand solicitud, CancellationToken ct)
        {
            Recibido = solicitud;
            var ficha = new FichaNutricionalDto(
                solicitud.ItemId, "Plato", "compuesto", null, null, null, null, null, null, DateTime.UtcNow);
            await _almacen!.GuardarAsync(ficha, ct);
            return ficha;
        }
    }

    private sealed class ObtenerFichaFalso(FichaNutricionalDto ficha) : IHandler<ObtenerFichaQuery, FichaNutricionalDto>
    {
        public Task<FichaNutricionalDto> HandleAsync(ObtenerFichaQuery solicitud, CancellationToken ct) => Task.FromResult(ficha);
    }
}
