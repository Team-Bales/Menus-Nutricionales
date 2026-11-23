using System.Net;
using System.Net.Http.Json;
using System.Text;
using Bunit;
using Bunit.TestDoubles;
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
    public async Task AutorizacionHandler_agrega_el_jwt_vigente_a_las_peticiones()
    {
        var token = CrearToken(DateTimeOffset.UtcNow.AddMinutes(5), "Admin");
        using var contexto = new BunitContext();
        var interno = new HandlerDeMensajesFalso(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var cliente = CrearCliente(contexto, new AlmacenDeSesionFalso(token), new SesionAutenticadaFalsa(), interno);

        await cliente.GetAsync("api/auth/perfil");

        Assert.Equal("Bearer", interno.Recibida!.Headers.Authorization!.Scheme);
        Assert.Equal(token, interno.Recibida.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task AutorizacionHandler_no_agrega_cabecera_si_no_hay_sesion()
    {
        using var contexto = new BunitContext();
        var interno = new HandlerDeMensajesFalso(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var cliente = CrearCliente(contexto, new AlmacenDeSesionFalso(), new SesionAutenticadaFalsa(), interno);

        await cliente.GetAsync("api/auth/perfil");

        Assert.Null(interno.Recibida!.Headers.Authorization);
    }

    [Fact]
    public async Task AutorizacionHandler_no_manda_y_borra_un_token_caducado()
    {
        using var contexto = new BunitContext();
        var almacen = new AlmacenDeSesionFalso(CrearToken(DateTimeOffset.UtcNow.AddMinutes(-1), "Admin"));
        var interno = new HandlerDeMensajesFalso(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var cliente = CrearCliente(contexto, almacen, new SesionAutenticadaFalsa(), interno);

        await cliente.GetAsync("api/alimentos");

        Assert.Null(interno.Recibida!.Headers.Authorization);
        Assert.Null(almacen.Token);
    }

    [Fact]
    public async Task AutorizacionHandler_cierra_la_sesion_y_va_al_login_si_la_api_rechaza_el_token()
    {
        using var contexto = new BunitContext();
        var sesion = new SesionAutenticadaFalsa();
        var almacen = new AlmacenDeSesionFalso(CrearToken(DateTimeOffset.UtcNow.AddMinutes(5), "Admin"));
        var interno = new HandlerDeMensajesFalso(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var cliente = CrearCliente(contexto, almacen, sesion, interno);
        var navegacion = contexto.Services.GetRequiredService<BunitNavigationManager>();
        navegacion.NavigateTo("/catalogo/dietas?page=2");

        var respuesta = await cliente.GetAsync("api/dietas");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.True(sesion.SeCerro);
        Assert.EndsWith("/login?returnUrl=%2Fcatalogo%2Fdietas%3Fpage%3D2", navegacion.Uri);
    }

    [Fact]
    public async Task AutorizacionHandler_no_cierra_la_sesion_ante_un_401_sin_token()
    {
        using var contexto = new BunitContext();
        var sesion = new SesionAutenticadaFalsa();
        var interno = new HandlerDeMensajesFalso(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var cliente = CrearCliente(contexto, new AlmacenDeSesionFalso(), sesion, interno);
        var navegacion = contexto.Services.GetRequiredService<BunitNavigationManager>();
        navegacion.NavigateTo("/login");

        await cliente.PostAsync("api/auth/login", null);

        Assert.False(sesion.SeCerro);
        Assert.EndsWith("/login", navegacion.Uri);
    }

    [Fact]
    public async Task AutorizacionHandler_no_cierra_la_sesion_ante_un_403()
    {
        using var contexto = new BunitContext();
        var sesion = new SesionAutenticadaFalsa();
        var almacen = new AlmacenDeSesionFalso(CrearToken(DateTimeOffset.UtcNow.AddMinutes(5), "Nutricionista"));
        var interno = new HandlerDeMensajesFalso(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        using var cliente = CrearCliente(contexto, almacen, sesion, interno);

        await cliente.PostAsync("api/dietas", null);

        Assert.False(sesion.SeCerro);
    }

    [Fact]
    public async Task ProveedorDeEstadoDeAutenticacion_borra_del_almacen_un_token_caducado()
    {
        var almacen = new AlmacenDeSesionFalso(CrearToken(DateTimeOffset.UtcNow.AddMinutes(-1), "Paciente"));

        await new ProveedorDeEstadoDeAutenticacion(almacen).GetAuthenticationStateAsync();

        Assert.Null(almacen.Token);
    }

    [Fact]
    public async Task ProveedorDeEstadoDeAutenticacion_reconoce_varios_roles_en_un_array()
    {
        var token = CrearTokenConPayload($$"""{"exp":{{DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()}},"role":["Admin","Nutricionista"],"jefe_nutricion":true}""");

        var estado = await new ProveedorDeEstadoDeAutenticacion(new AlmacenDeSesionFalso(token)).GetAuthenticationStateAsync();

        Assert.True(estado.User.IsInRole("Admin"));
        Assert.True(estado.User.IsInRole("Nutricionista"));
        Assert.Equal("True", estado.User.FindFirst("jefe_nutricion")!.Value);
    }

    [Fact]
    public async Task ProveedorDeEstadoDeAutenticacion_rechaza_un_payload_que_no_es_un_objeto()
    {
        var estado = await new ProveedorDeEstadoDeAutenticacion(new AlmacenDeSesionFalso(CrearTokenConPayload("[1,2]"))).GetAuthenticationStateAsync();

        Assert.False(estado.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task IniciarSesionAsync_con_un_token_invalido_publica_el_estado_anonimo()
    {
        var proveedor = new ProveedorDeEstadoDeAutenticacion(new AlmacenDeSesionFalso());
        Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>? publicado = null;
        proveedor.AuthenticationStateChanged += estado => publicado = estado;

        await proveedor.IniciarSesionAsync("no-es-un-jwt");

        Assert.False((await publicado!).User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task IniciarSesionAsync_usa_un_mensaje_generico_si_detail_no_es_texto()
    {
        var handler = new HandlerDeMensajesFalso(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"detail":42}""", Encoding.UTF8, "application/problem+json"),
        });

        var error = await Assert.ThrowsAsync<ApiException>(() => CrearApi(handler).IniciarSesionAsync(new CredencialesLogin()));

        Assert.Equal("La API respondió 400.", error.Message);
    }

    [Fact]
    public async Task ProveedorDeEstadoDeAutenticacion_sin_token_es_anonimo_y_no_toca_el_almacen()
    {
        var almacen = new AlmacenDeSesionFalso();

        var estado = await new ProveedorDeEstadoDeAutenticacion(almacen).GetAuthenticationStateAsync();

        Assert.False(estado.User.Identity!.IsAuthenticated);
        Assert.Equal(0, almacen.Eliminaciones);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ProveedorDeEstadoDeAutenticacion_trata_un_token_vacio_como_anonimo_y_lo_borra(string token)
    {
        var almacen = new AlmacenDeSesionFalso(token);

        var estado = await new ProveedorDeEstadoDeAutenticacion(almacen).GetAuthenticationStateAsync();

        Assert.False(estado.User.Identity!.IsAuthenticated);
        Assert.Null(almacen.Token);
    }

    [Theory]
    [InlineData("""{"role":"Admin"}""")]
    [InlineData("""{"exp":"mañana","role":"Admin"}""")]
    public async Task ProveedorDeEstadoDeAutenticacion_rechaza_y_borra_un_token_sin_exp_valido(string payload)
    {
        var almacen = new AlmacenDeSesionFalso(CrearTokenConPayload(payload));

        var estado = await new ProveedorDeEstadoDeAutenticacion(almacen).GetAuthenticationStateAsync();

        Assert.False(estado.User.Identity!.IsAuthenticated);
        Assert.Null(almacen.Token);
    }

    [Fact]
    public async Task ProveedorDeEstadoDeAutenticacion_ignora_los_claims_que_no_son_valores_simples()
    {
        var exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        var token = CrearTokenConPayload($$"""{"exp":{{exp}},"role":["Nutricionista",{"anidado":1},null],"perfil":{"sala":"A"},"vacio":null}""");

        var estado = await new ProveedorDeEstadoDeAutenticacion(new AlmacenDeSesionFalso(token)).GetAuthenticationStateAsync();

        Assert.True(estado.User.IsInRole("Nutricionista"));
        Assert.Single(estado.User.FindAll("role"));
        Assert.Null(estado.User.FindFirst("perfil"));
        Assert.Null(estado.User.FindFirst("vacio"));
    }

    [Fact]
    public async Task IniciarSesionAsync_rechaza_una_respuesta_exitosa_vacia()
    {
        var handler = new HandlerDeMensajesFalso(_ => Json("null"));

        var error = await Assert.ThrowsAsync<ApiException>(() => CrearApi(handler).IniciarSesionAsync(new CredencialesLogin()));

        Assert.Equal("La API devolvió una respuesta vacía.", error.Message);
    }

    [Fact]
    public async Task AlmacenDeSesion_lee_localStorage_una_sola_vez_y_despues_usa_la_memoria()
    {
        using var contexto = new BunitContext();
        contexto.JSInterop.Setup<string?>("localStorage.getItem", _ => true).SetResult("token-almacenado");
        var almacen = new AlmacenDeSesion(contexto.Services.GetRequiredService<IJSRuntime>());

        var primero = await almacen.ObtenerTokenAsync();
        var segundo = await almacen.ObtenerTokenAsync();

        Assert.Equal("token-almacenado", primero);
        Assert.Equal("token-almacenado", segundo);
        Assert.Single(contexto.JSInterop.Invocations);
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
        Assert.Null(await almacen.ObtenerTokenAsync());
    }

    private static AutenticacionApi CrearApi(HttpMessageHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

    private static HttpResponseMessage Json(string contenido)
        => new(HttpStatusCode.OK) { Content = new StringContent(contenido, Encoding.UTF8, "application/json") };

    private static string CrearToken(DateTimeOffset expira, string rol)
        => CrearTokenConPayload($$"""{"exp":{{expira.ToUnixTimeSeconds()}},"role":"{{rol}}"}""");

    private static string CrearTokenConPayload(string json)
    {
        static string Base64Url(string texto) => Convert.ToBase64String(Encoding.UTF8.GetBytes(texto))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{Base64Url("{}")}.{Base64Url(json)}.firma";
    }

    private static HttpClient CrearCliente(BunitContext contexto, IAlmacenDeSesion almacen, ISesionAutenticada sesion, HttpMessageHandler interno)
    {
        contexto.Services.AddSingleton(sesion);
        var handler = new AutorizacionHandler(almacen, contexto.Services) { InnerHandler = interno };
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
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
