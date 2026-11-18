using System.Net;
using System.Net.Http.Json;
using System.Text;
using Bunit;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Menus.Client.Tests.Services;

/// <summary>Pruebas del contrato HTTP y la persistencia local de autenticación (issue #75).</summary>
public sealed class AutenticacionTests
{
    [Fact]
    public async Task IniciarSesionAsync_envia_credenciales_a_la_ruta_de_login()
    {
        var handler = new HandlerDeMensajesFalso(_ => Json("""{"token":"jwt","expira":"2026-12-01T00:00:00Z","rol":"Admin"}"""));
        var api = CrearApi(handler);

        var resultado = await api.IniciarSesionAsync(new CredencialesLogin { Email = "admin@centro.test", Password = "secreto" });

        Assert.Equal(HttpMethod.Post, handler.Recibida!.Method);
        Assert.Equal("/api/auth/login", handler.Recibida.RequestUri!.AbsolutePath);
        Assert.Equal("jwt", resultado.Token);
        var cuerpo = await handler.Recibida.Content!.ReadFromJsonAsync<CredencialesLogin>();
        Assert.Equal("admin@centro.test", cuerpo!.Email);
    }

    [Fact]
    public async Task ObtenerPerfilAsync_envia_get_a_la_ruta_protegida()
    {
        var entidadId = Guid.NewGuid();
        var handler = new HandlerDeMensajesFalso(_ => Json($$"""{"email":"nutri@centro.test","rol":"Nutricionista","entidadId":"{{entidadId}}"}"""));
        var api = CrearApi(handler);

        var perfil = await api.ObtenerPerfilAsync();

        Assert.Equal(HttpMethod.Get, handler.Recibida!.Method);
        Assert.Equal("/api/auth/perfil", handler.Recibida.RequestUri!.AbsolutePath);
        Assert.Equal(entidadId, perfil.EntidadId);
    }

    [Fact]
    public async Task IniciarSesionAsync_proporciona_el_detalle_del_error_de_la_api()
    {
        var handler = new HandlerDeMensajesFalso(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = JsonContent.Create(new { detail = "Credenciales inválidas." }),
        });

        var error = await Assert.ThrowsAsync<ApiException>(() => CrearApi(handler).IniciarSesionAsync(new CredencialesLogin()));

        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        Assert.Equal("Credenciales inválidas.", error.Message);
    }

    [Fact]
    public async Task IniciarSesionAsync_usa_un_mensaje_generico_si_el_error_no_es_json()
    {
        var handler = new HandlerDeMensajesFalso(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error no estructurado", Encoding.UTF8, "text/plain"),
        });

        var error = await Assert.ThrowsAsync<ApiException>(() => CrearApi(handler).IniciarSesionAsync(new CredencialesLogin()));

        Assert.Equal("La API respondió 500.", error.Message);
    }

    [Fact]
    public async Task ObtenerPerfilAsync_rechaza_una_respuesta_exitosa_vacia()
    {
        var handler = new HandlerDeMensajesFalso(_ => Json("null"));

        await Assert.ThrowsAsync<ApiException>(() => CrearApi(handler).ObtenerPerfilAsync());
    }

    [Fact]
    public async Task ProveedorDeEstadoDeAutenticacion_crea_un_usuario_con_el_rol_del_token()
    {
        var almacen = new AlmacenDeSesionFalso(CrearToken(DateTimeOffset.UtcNow.AddMinutes(5), "Admin"));
        var proveedor = new ProveedorDeEstadoDeAutenticacion(almacen);

        var estado = await proveedor.GetAuthenticationStateAsync();

        Assert.True(estado.User.Identity!.IsAuthenticated);
        Assert.True(estado.User.IsInRole("Admin"));
    }

    [Fact]
    public async Task ProveedorDeEstadoDeAutenticacion_rechaza_un_token_expirado()
    {
        var almacen = new AlmacenDeSesionFalso(CrearToken(DateTimeOffset.UtcNow.AddMinutes(-1), "Paciente"));
        var proveedor = new ProveedorDeEstadoDeAutenticacion(almacen);

        var estado = await proveedor.GetAuthenticationStateAsync();

        Assert.False(estado.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task ProveedorDeEstadoDeAutenticacion_rechaza_un_token_malformado()
    {
        var proveedor = new ProveedorDeEstadoDeAutenticacion(new AlmacenDeSesionFalso("no-es-un-jwt"));

        var estado = await proveedor.GetAuthenticationStateAsync();

        Assert.False(estado.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task ProveedorDeEstadoDeAutenticacion_rechaza_un_payload_codificado_de_forma_invalida()
    {
        var proveedor = new ProveedorDeEstadoDeAutenticacion(new AlmacenDeSesionFalso("cabecera.! .firma".Replace(" ", string.Empty)));

        var estado = await proveedor.GetAuthenticationStateAsync();

        Assert.False(estado.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task ProveedorDeEstadoDeAutenticacion_rechaza_un_payload_que_no_es_json()
    {
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{"))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var proveedor = new ProveedorDeEstadoDeAutenticacion(new AlmacenDeSesionFalso($"cabecera.{payload}.firma"));

        var estado = await proveedor.GetAuthenticationStateAsync();

        Assert.False(estado.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task IniciarSesionAsync_guarda_el_token_en_el_almacen()
    {
        var almacen = new AlmacenDeSesionFalso();
        var proveedor = new ProveedorDeEstadoDeAutenticacion(almacen);
        var token = CrearToken(DateTimeOffset.UtcNow.AddMinutes(5), "Paciente");

        await proveedor.IniciarSesionAsync(token);

        Assert.Equal(token, almacen.Token);
    }

    [Fact]
    public async Task CerrarSesionAsync_elimina_el_token_del_almacen()
    {
        var almacen = new AlmacenDeSesionFalso(CrearToken(DateTimeOffset.UtcNow.AddMinutes(5), "Paciente"));
        var proveedor = new ProveedorDeEstadoDeAutenticacion(almacen);

        await proveedor.CerrarSesionAsync();

        Assert.Null(almacen.Token);
    }

    [Fact]
    public async Task AutorizacionHandler_agrega_el_jwt_a_las_peticiones()
    {
        var almacen = new AlmacenDeSesionFalso("jwt-de-prueba");
        var interno = new HandlerDeMensajesFalso(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var cliente = new HttpClient(new AutorizacionHandler(almacen) { InnerHandler = interno }) { BaseAddress = new Uri("http://localhost/") };

        await cliente.GetAsync("api/auth/perfil");

        Assert.Equal("Bearer", interno.Recibida!.Headers.Authorization!.Scheme);
        Assert.Equal("jwt-de-prueba", interno.Recibida.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task AutorizacionHandler_no_agrega_cabecera_si_no_hay_sesion()
    {
        var interno = new HandlerDeMensajesFalso(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var cliente = new HttpClient(new AutorizacionHandler(new AlmacenDeSesionFalso()) { InnerHandler = interno }) { BaseAddress = new Uri("http://localhost/") };

        await cliente.GetAsync("api/auth/perfil");

        Assert.Null(interno.Recibida!.Headers.Authorization);
    }

    [Fact]
    public async Task AlmacenDeSesion_usa_localStorage_para_guardar_leer_y_eliminar_el_token()
    {
        using var contexto = new BunitContext();
        contexto.JSInterop.Setup<string?>("localStorage.getItem", invocation => invocation.Arguments[0]!.ToString() == "menus-nutricionales.token")
            .SetResult("token-almacenado");
        contexto.JSInterop.SetupVoid("localStorage.setItem", invocation => invocation.Arguments[0]!.ToString() == "menus-nutricionales.token")
            .SetVoidResult();
        contexto.JSInterop.SetupVoid("localStorage.removeItem", invocation => invocation.Arguments[0]!.ToString() == "menus-nutricionales.token")
            .SetVoidResult();
        var almacen = new AlmacenDeSesion(contexto.Services.GetRequiredService<IJSRuntime>());

        var token = await almacen.ObtenerTokenAsync();
        await almacen.GuardarTokenAsync("nuevo-token");
        await almacen.EliminarTokenAsync();

        Assert.Equal("token-almacenado", token);
        Assert.Equal(3, contexto.JSInterop.Invocations.Count);
    }

    private static AutenticacionApi CrearApi(HttpMessageHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

    private static HttpResponseMessage Json(string contenido)
        => new(HttpStatusCode.OK) { Content = new StringContent(contenido, Encoding.UTF8, "application/json") };

    private static string CrearToken(DateTimeOffset expira, string rol)
    {
        var cabecera = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"));
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($$"""{"exp":{{expira.ToUnixTimeSeconds()}},"role":"{{rol}}"}"""));
        return $"{cabecera.TrimEnd('=').Replace('+', '-').Replace('/', '_')}.{payload.TrimEnd('=').Replace('+', '-').Replace('/', '_')}.firma";
    }

    private sealed class HandlerDeMensajesFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? Recibida { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Recibida = request;
            return Task.FromResult(responder(request));
        }
    }
}
