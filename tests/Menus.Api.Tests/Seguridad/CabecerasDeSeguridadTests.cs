using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Menus.Api.Seguridad;
using Menus.Domain;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Menus.Api.Tests.Seguridad;

/// <summary>
/// Pruebas de las cabeceras de seguridad de la API (#294, §13.3): HSTS sobre HTTPS y fuera de desarrollo, y las
/// cabeceras que acompañan a todas las respuestas, incluidas las de error.
/// </summary>
public sealed class CabecerasDeSeguridadTests
{
    private static HttpClient ClienteHttps(WebApplicationFactory<Program> api)
        => api.CreateClient(new WebApplicationFactoryClientOptions
        {
            // Un host que no esté en HstsOptions.ExcludedHosts (localhost lo está por defecto).
            BaseAddress = new Uri("https://menus.example"),
            AllowAutoRedirect = false,
        });

    private static string? Cabecera(HttpResponseMessage respuesta, string nombre)
        => respuesta.Headers.TryGetValues(nombre, out var valores) ? valores.Single() : null;

    [Fact]
    public async Task Una_respuesta_correcta_sobre_https_lleva_HSTS_y_las_demas_cabeceras()
    {
        await using var api = ApiEnMemoria.Crear();
        using var cliente = ClienteHttps(api);
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiEnMemoria.Token(RolesUsuario.Admin));

        using var respuesta = await cliente.GetAsync("/api/nomencladores/tipos-preparacion");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("nosniff", Cabecera(respuesta, "X-Content-Type-Options"));
        Assert.Equal("DENY", Cabecera(respuesta, "X-Frame-Options"));
        Assert.Equal("no-referrer", Cabecera(respuesta, "Referrer-Policy"));
        Assert.Contains("camera=()", Cabecera(respuesta, "Permissions-Policy"));
        Assert.Contains("default-src 'none'", Cabecera(respuesta, "Content-Security-Policy"));
        Assert.Contains("frame-ancestors 'none'", Cabecera(respuesta, "Content-Security-Policy"));
        Assert.Contains("max-age=31536000", Cabecera(respuesta, "Strict-Transport-Security"));
        Assert.Contains("includeSubDomains", Cabecera(respuesta, "Strict-Transport-Security"));
    }

    [Fact]
    public async Task Los_401_y_403_tambien_llevan_las_cabeceras()
    {
        await using var api = ApiEnMemoria.Crear();
        using var cliente = ClienteHttps(api);

        using var sinToken = await cliente.GetAsync("/api/alimentos");
        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);
        Assert.Equal("nosniff", Cabecera(sinToken, "X-Content-Type-Options"));
        Assert.NotNull(Cabecera(sinToken, "Content-Security-Policy"));

        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/alimentos")
        {
            Content = JsonContent.Create(new { }),
        };
        peticion.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiEnMemoria.Token(RolesUsuario.Paciente));

        using var sinPermiso = await cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.Forbidden, sinPermiso.StatusCode);
        Assert.Equal("nosniff", Cabecera(sinPermiso, "X-Content-Type-Options"));
        Assert.NotNull(Cabecera(sinPermiso, "Content-Security-Policy"));
    }

    [Fact]
    public async Task En_Development_no_se_anuncia_HSTS()
    {
        await using var api = ApiEnMemoria.Crear("Development");
        using var cliente = ClienteHttps(api);

        using var respuesta = await cliente.GetAsync("/api/nomencladores/tipos-preparacion");

        Assert.Null(Cabecera(respuesta, "Strict-Transport-Security"));
        Assert.NotNull(Cabecera(respuesta, "Content-Security-Policy"));
    }

    [Fact]
    public async Task Sobre_http_no_se_anuncia_HSTS()
    {
        await using var api = ApiEnMemoria.Crear();
        using var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiEnMemoria.Token(RolesUsuario.Admin));

        using var respuesta = await cliente.GetAsync("/api/nomencladores/tipos-preparacion");

        Assert.Null(Cabecera(respuesta, "Strict-Transport-Security"));
    }

    [Fact]
    public async Task Un_host_excluido_no_recibe_HSTS()
    {
        await using var api = ApiEnMemoria.Crear();
        using var cliente = api.CreateClient(new WebApplicationFactoryClientOptions
        {
            // localhost está en HstsOptions.ExcludedHosts por defecto.
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiEnMemoria.Token(RolesUsuario.Admin));

        using var respuesta = await cliente.GetAsync("/api/nomencladores/tipos-preparacion");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null(Cabecera(respuesta, "Strict-Transport-Security"));
        Assert.Equal("nosniff", Cabecera(respuesta, "X-Content-Type-Options"));
    }

    [Theory]
    [InlineData(365, true, false, "max-age=31536000; includeSubDomains")]
    [InlineData(365, false, false, "max-age=31536000")]
    [InlineData(365, true, true, "max-age=31536000; includeSubDomains; preload")]
    [InlineData(365, false, true, "max-age=31536000; preload")]
    public void El_valor_de_HSTS_respeta_las_opciones(int dias, bool subdominios, bool preload, string esperado)
    {
        var opciones = new HstsOptions
        {
            MaxAge = TimeSpan.FromDays(dias),
            IncludeSubDomains = subdominios,
            Preload = preload,
        };

        Assert.Equal(esperado, CabecerasDeSeguridad.ArmarHsts(opciones));
    }
}
