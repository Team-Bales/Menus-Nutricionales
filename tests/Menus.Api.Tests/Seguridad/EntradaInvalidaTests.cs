using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Menus.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Tests.Seguridad;

/// <summary>
/// Pruebas de la validación de entrada de la API (#294, §13.3): lo malformado responde 400 con un mensaje genérico, y
/// la validación de negocio de FluentValidation sigue respondiendo 422 (ADR-010, §3).
/// </summary>
public sealed class EntradaInvalidaTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static async Task<ProblemDetails> LeerProblemaAsync(HttpResponseMessage respuesta)
    {
        var texto = await respuesta.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ProblemDetails>(texto, Json)!;
    }

    [Theory]
    [InlineData("{ \"email\": ")]
    [InlineData("{\"email\": 42, \"password\": true}")]
    [InlineData("[]")]
    [InlineData("null")]
    public async Task Una_entrada_malformada_responde_400_con_un_mensaje_generico(string cuerpo)
    {
        await using var api = ApiEnMemoria.Crear();
        using var respuesta = await api.CreateClient().PostAsync(
            "/api/auth/login",
            new StringContent(cuerpo, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType!.MediaType);

        var texto = await respuesta.Content.ReadAsStringAsync();
        var problema = JsonSerializer.Deserialize<ProblemDetails>(texto, Json)!;

        Assert.Equal(400, problema.Status);
        Assert.Equal("La solicitud no es válida.", problema.Detail);
        Assert.False(problema.Extensions.ContainsKey("errores"));
        Assert.DoesNotContain("Json", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Un_cuerpo_ausente_responde_400_generico()
    {
        await using var api = ApiEnMemoria.Crear();
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json"),
        };
        using var respuesta = await api.CreateClient().SendAsync(peticion);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("La solicitud no es válida.", (await LeerProblemaAsync(respuesta)).Detail);
    }

    [Fact]
    public async Task La_validacion_de_negocio_sigue_siendo_422()
    {
        // El cuerpo tiene la forma correcta; los campos vacíos los rechaza FluentValidation, no el enlace del modelo.
        await using var api = ApiEnMemoria.Crear();
        using var respuesta = await api.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        var problema = await LeerProblemaAsync(respuesta);

        Assert.Equal(422, problema.Status);
        Assert.Contains("errores", problema.Extensions);
    }

    [Fact]
    public async Task Un_token_invalido_no_es_un_error_de_entrada_sino_401()
    {
        await using var api = ApiEnMemoria.Crear();
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/alimentos");
        peticion.Headers.Authorization = new("Bearer", "no-es-un-token");

        using var respuesta = await api.CreateClient().SendAsync(peticion);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
