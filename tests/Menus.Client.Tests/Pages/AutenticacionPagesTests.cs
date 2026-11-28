using System.Net;
using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Layout;
using Menus.Client.Pages;
using Menus.Client.Services;
using Menus.Client.Tests.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de las pantallas de inicio y cierre de sesión (issue #75).</summary>
public sealed class AutenticacionPagesTests : BunitContext
{
    [Fact]
    public void Login_con_credenciales_validas_guarda_el_token_y_vuelve_al_inicio()
    {
        var sesion = new SesionAutenticadaFalsa();
        var api = new AutenticacionApiFalsa
        {
            ResultadoDeLogin = new ResultadoLogin("jwt"),
        };
        Servicios(api, sesion);
        var cut = Render<Login>();

        cut.Find("#email").Change("admin@centro.test");
        cut.Find("#password").Change("secreto");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal("jwt", sesion.TokenIniciado));
        Assert.Equal("admin@centro.test", api.CredencialesRecibidas!.Email);
        Assert.EndsWith("/", Services.GetRequiredService<BunitNavigationManager>().Uri);
    }

    [Fact]
    public void Login_con_credenciales_invalidas_muestra_el_error_de_la_api()
    {
        var api = new AutenticacionApiFalsa
        {
            ErrorDeLogin = new ApiException(HttpStatusCode.Unauthorized, "Credenciales inválidas."),
        };
        Servicios(api, new SesionAutenticadaFalsa());
        var cut = Render<Login>();

        cut.Find("#email").Change("admin@centro.test");
        cut.Find("#password").Change("incorrecta");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Credenciales inválidas.", cut.Markup));
    }

    [Fact]
    public void Perfil_muestra_los_datos_basicos_y_cierra_la_sesion()
    {
        var sesion = new SesionAutenticadaFalsa();
        var api = new AutenticacionApiFalsa
        {
            Perfil = new PerfilUsuario("nutri@centro.test", "Nutricionista", Guid.NewGuid()),
        };
        Servicios(api, sesion);
        var cut = Render<Perfil>();

        cut.WaitForAssertion(() => Assert.Contains("nutri@centro.test", cut.Markup));
        Assert.Contains("Nutricionista", cut.Markup);
        cut.Find("button").Click();

        cut.WaitForAssertion(() => Assert.True(sesion.SeCerro));
        Assert.EndsWith("/login", Services.GetRequiredService<BunitNavigationManager>().Uri);
    }

    [Fact]
    public void Perfil_muestra_el_error_si_no_puede_consultar_la_api()
    {
        Servicios(new AutenticacionApiFalsa
        {
            ErrorDePerfil = new ApiException(HttpStatusCode.Unauthorized, "La sesión no es válida."),
        }, new SesionAutenticadaFalsa());

        var cut = Render<Perfil>();

        cut.WaitForAssertion(() => Assert.Contains("La sesión no es válida.", cut.Markup));
    }

    [Fact]
    public void Perfil_muestra_el_estado_de_carga_hasta_que_la_api_responde()
    {
        var api = new AutenticacionApiPendiente();
        Servicios(api, new SesionAutenticadaFalsa());
        var cut = Render<Perfil>();

        Assert.Contains("Cargando perfil", cut.Markup);
        api.CompletarPerfil(new PerfilUsuario("paciente@centro.test", "Paciente", Guid.NewGuid()));

        cut.WaitForAssertion(() => Assert.Contains("paciente@centro.test", cut.Markup));
    }

    [Fact]
    public void RedirigirALogin_navega_a_la_pantalla_de_inicio_de_sesion()
    {
        Render<RedirigirALogin>();

        Assert.EndsWith("/login", Services.GetRequiredService<BunitNavigationManager>().Uri);
    }

    [Fact]
    public void RedirigirALogin_recuerda_la_pagina_pedida_en_returnUrl()
    {
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/catalogo/platos");

        Render<RedirigirALogin>();

        Assert.EndsWith("/login?returnUrl=%2Fcatalogo%2Fplatos", Services.GetRequiredService<BunitNavigationManager>().Uri);
    }

    [Fact]
    public void Login_vuelve_a_la_pagina_de_returnUrl()
    {
        var sesion = new SesionAutenticadaFalsa();
        Servicios(new AutenticacionApiFalsa { ResultadoDeLogin = new ResultadoLogin("jwt") }, sesion);
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/login?returnUrl=" + Uri.EscapeDataString("/catalogo/dietas"));
        var cut = Render<Login>();

        Enviar(cut);

        cut.WaitForAssertion(() => Assert.Equal("jwt", sesion.TokenIniciado));
        Assert.EndsWith("/catalogo/dietas", Services.GetRequiredService<BunitNavigationManager>().Uri);
    }

    [Fact]
    public void Login_ignora_un_returnUrl_externo_y_vuelve_al_inicio()
    {
        var sesion = new SesionAutenticadaFalsa();
        Servicios(new AutenticacionApiFalsa { ResultadoDeLogin = new ResultadoLogin("jwt") }, sesion);
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/login?returnUrl=" + Uri.EscapeDataString("https://otro-sitio.test/robar"));
        var cut = Render<Login>();

        Enviar(cut);

        cut.WaitForAssertion(() => Assert.Equal("jwt", sesion.TokenIniciado));
        Assert.Equal("http://localhost/", Services.GetRequiredService<BunitNavigationManager>().Uri);
    }

    [Theory]
    [InlineData(typeof(HttpRequestException))]
    [InlineData(typeof(TaskCanceledException))]
    public void Login_con_la_api_caida_muestra_un_mensaje_en_el_formulario(Type tipoDeError)
    {
        Servicios(new AutenticacionApiFalsa { ErrorDeLogin = (Exception)Activator.CreateInstance(tipoDeError)! }, new SesionAutenticadaFalsa());
        var cut = Render<Login>();

        Enviar(cut);

        cut.WaitForAssertion(() => Assert.Contains(MensajesDeConexion.SinServidor, cut.Markup));
        Assert.Null(cut.Find("button[type=submit]").GetAttribute("disabled"));
    }

    [Fact]
    public void Perfil_con_la_api_caida_muestra_un_mensaje()
    {
        Servicios(new AutenticacionApiFalsa { ErrorDePerfil = new HttpRequestException() }, new SesionAutenticadaFalsa());

        var cut = Render<Perfil>();

        cut.WaitForAssertion(() => Assert.Contains(MensajesDeConexion.SinServidor, cut.Markup));
    }

    [Fact]
    public void AccesoDenegado_explica_que_falta_el_rol_y_ofrece_volver()
    {
        var cut = Render<AccesoDenegado>();

        Assert.Contains("Sin permisos para esta página", cut.Markup);
        Assert.NotNull(cut.Find("a[href='']"));
    }

    [Fact]
    public void App_con_sesion_pero_sin_el_rol_muestra_sin_permisos_y_no_va_al_login()
    {
        AddAuthorization().SetAuthorized("paciente@centro.test").SetRoles(RolesDeUsuario.Paciente);
        Servicios(new AutenticacionApiFalsa(), new SesionAutenticadaFalsa());
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/admin/nomencladores");

        var cut = Render<App>();

        cut.WaitForAssertion(() => Assert.Contains("Sin permisos para esta página", cut.Markup));
        Assert.EndsWith("/admin/nomencladores", Services.GetRequiredService<BunitNavigationManager>().Uri);
    }

    [Fact]
    public void App_sin_sesion_redirige_al_login_recordando_la_pagina()
    {
        AddAuthorization();
        Servicios(new AutenticacionApiFalsa(), new SesionAutenticadaFalsa());
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/admin/nomencladores");

        Render<App>();

        Assert.EndsWith("/login?returnUrl=%2Fadmin%2Fnomencladores", Services.GetRequiredService<BunitNavigationManager>().Uri);
    }

    [Fact]
    public void LayoutVacio_muestra_el_contenido_de_la_pagina()
    {
        RenderFragment contenido = builder => builder.AddMarkupContent(0, "<p>Inicio de sesión</p>");

        var cut = Render<LayoutVacio>(parameters => parameters.Add(layout => layout.Body, contenido));

        Assert.Contains("Inicio de sesión", cut.Markup);
    }

    [Fact]
    public void App_con_un_usuario_sin_identidad_lo_trata_como_anonimo_y_va_al_login()
    {
        // ClaimsPrincipal sin identidades: User.Identity es null, un caso que el AddAuthorization de
        // bUnit no produce. Se usa su servicio de autorización y se sustituye solo el estado.
        AddAuthorization();
        Services.AddSingleton<AuthenticationStateProvider>(new EstadoFijo(new ClaimsPrincipal()));
        Servicios(new AutenticacionApiFalsa(), new SesionAutenticadaFalsa());
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/perfil");

        Render<App>();

        Assert.EndsWith("/login?returnUrl=%2Fperfil", Services.GetRequiredService<BunitNavigationManager>().Uri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void La_nota_de_credenciales_de_ejemplo_solo_se_muestra_en_desarrollo(bool desarrollo)
    {
        Servicios(new AutenticacionApiFalsa(), new SesionAutenticadaFalsa(), desarrollo);

        var cut = Render<Login>();

        if (desarrollo)
        {
            Assert.Contains("datos de ejemplo", cut.Markup);
        }
        else
        {
            Assert.DoesNotContain("datos de ejemplo", cut.Markup);
        }
    }

    [Fact]
    public void La_pantalla_de_acceso_incluye_a_los_pacientes_y_empieza_por_el_h1()
    {
        Servicios(new AutenticacionApiFalsa(), new SesionAutenticadaFalsa());

        var cut = Render<Login>();

        Assert.Contains("Acceso para el personal y los pacientes del centro.", cut.Markup);
        Assert.DoesNotContain("restringido al personal", cut.Markup);
        Assert.Equal("Te damos la bienvenida", cut.Find("h1").TextContent);
        Assert.Empty(cut.FindAll(".login-brand-center h2"));
    }

    private sealed class EntornoFalso(string environment) : IWebAssemblyHostEnvironment
    {
        public string BaseAddress => "http://localhost/";
        public string Environment { get; } = environment;
    }

    private sealed class EstadoFijo(ClaimsPrincipal usuario) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(usuario));
    }

    private static void Enviar(IRenderedComponent<Login> cut)
    {
        cut.Find("#email").Change("admin@centro.test");
        cut.Find("#password").Change("secreto");
        cut.Find("form").Submit();
    }

    private void Servicios(IAutenticacionApi api, ISesionAutenticada sesion, bool desarrollo = false)
    {
        Services.AddSingleton(api);
        Services.AddSingleton(sesion);
        Services.AddSingleton<IWebAssemblyHostEnvironment>(
            new EntornoFalso(desarrollo ? "Development" : "Production"));
    }
}
