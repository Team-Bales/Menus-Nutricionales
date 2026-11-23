using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Menus.Api.Seguridad;
using Menus.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Menus.Api.Tests.Seguridad;

/// <summary>
/// Pruebas del límite de peticiones de los endpoints de autenticación (#294, §13.3): al superar la ventana, la API
/// responde 429 con <c>ProblemDetails</c> y <c>Retry-After</c>; los demás endpoints no se ven afectados.
/// </summary>
public sealed class LimiteDePeticionesTests
{
    private static WebApplicationFactory<Program> Api(int permitLimit)
        => ApiEnMemoria.Crear(
            "Production",
            ("RateLimiting:PermitLimit", permitLimit.ToString()),
            ("RateLimiting:WindowSeconds", "60"));

    [Fact]
    public async Task El_login_responde_429_al_superar_el_limite()
    {
        const int limite = 3;
        await using var api = Api(limite);
        using var cliente = api.CreateClient();

        for (var intento = 0; intento < limite; intento++)
        {
            using var permitida = await cliente.PostAsJsonAsync(
                "/api/auth/login", new { email = "", password = "" });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, permitida.StatusCode);
        }

        using var rechazada = await cliente.PostAsJsonAsync(
            "/api/auth/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.TooManyRequests, rechazada.StatusCode);
        Assert.True(rechazada.Headers.Contains("Retry-After"));
        Assert.Equal("application/problem+json", rechazada.Content.Headers.ContentType!.MediaType);

        var problema = await rechazada.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(429, problema!.Status);
        Assert.Equal("Demasiadas solicitudes. Inténtalo de nuevo más tarde.", problema.Detail);
    }

    [Fact]
    public async Task El_limite_no_afecta_a_los_demas_endpoints()
    {
        const int limite = 2;
        await using var api = Api(limite);
        using var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiEnMemoria.Token(RolesUsuario.Admin));

        // El límite sigue activo en auth en la misma instancia: si no, el test anterior no probaría nada.
        for (var intento = 0; intento < limite; intento++)
        {
            using var permitida = await cliente.PostAsJsonAsync(
                "/api/auth/login", new { email = "", password = "" });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, permitida.StatusCode);
        }

        using var rechazada = await cliente.PostAsJsonAsync(
            "/api/auth/login", new { email = "", password = "" });
        Assert.Equal(HttpStatusCode.TooManyRequests, rechazada.StatusCode);

        // El mismo origen ya agotó el login, pero otro endpoint sigue respondiendo (422 de validación).
        using var otro = await cliente.PostAsJsonAsync(
            "/api/nomencladores/grupos-nutricionales", new { });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, otro.StatusCode);
    }

    [Fact]
    public void La_clave_de_particion_usa_la_direccion_de_origen()
    {
        Assert.Equal("203.0.113.7", LimiteDePeticiones.ClaveDeOrigen(IPAddress.Parse("203.0.113.7")));
        Assert.Equal("sin-direccion", LimiteDePeticiones.ClaveDeOrigen(null));
    }

    [Fact]
    public void Sin_espera_en_el_arrendamiento_se_anuncia_la_ventana()
    {
        Assert.Equal("60", LimiteDePeticiones.SegundosDeEspera(new ArrendamientoSinEspera(), 60));
    }

    [Theory]
    [InlineData("/api/auth/registro/nutricionista")]
    [InlineData("/api/auth/registro/paciente")]
    public async Task El_registro_no_esta_limitado(string ruta)
    {
        const int limite = 2;
        await using var api = Api(limite);
        using var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiEnMemoria.Token(RolesUsuario.Admin));

        // Agota el límite del login: el contador de la IP queda lleno antes de tocar el registro.
        for (var intento = 0; intento < limite; intento++)
        {
            using var permitida = await cliente.PostAsJsonAsync(
                "/api/auth/login", new { email = "", password = "" });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, permitida.StatusCode);
        }

        using var rechazada = await cliente.PostAsJsonAsync(
            "/api/auth/login", new { email = "", password = "" });
        Assert.Equal(HttpStatusCode.TooManyRequests, rechazada.StatusCode);

        for (var intento = 0; intento < limite + 3; intento++)
        {
            using var respuesta = await cliente.PostAsJsonAsync(ruta, new { });
            Assert.NotEqual(HttpStatusCode.TooManyRequests, respuesta.StatusCode);
        }
    }

    private sealed class ArrendamientoSinEspera : RateLimitLease
    {
        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }
}
