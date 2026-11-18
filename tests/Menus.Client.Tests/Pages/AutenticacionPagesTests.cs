using System.Net;
using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Layout;
using Menus.Client.Pages;
using Menus.Client.Services;
using Menus.Client.Tests.Services;
using Microsoft.AspNetCore.Components;
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
            ResultadoDeLogin = new ResultadoLogin("jwt", DateTime.UtcNow.AddMinutes(10), "Admin"),
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
    public void LayoutVacio_muestra_el_contenido_de_la_pagina()
    {
        RenderFragment contenido = builder => builder.AddMarkupContent(0, "<p>Inicio de sesión</p>");

        var cut = Render<LayoutVacio>(parameters => parameters.Add(layout => layout.Body, contenido));

        Assert.Contains("Inicio de sesión", cut.Markup);
    }

    private void Servicios(IAutenticacionApi api, ISesionAutenticada sesion)
    {
        Services.AddSingleton(api);
        Services.AddSingleton(sesion);
    }
}
