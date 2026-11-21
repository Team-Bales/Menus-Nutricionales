using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Services;

/// <summary>Pruebas del <c>returnUrl</c> del login (#75): solo se vuelve a rutas locales.</summary>
public sealed class RutasDeSesionTests : BunitContext
{
    [Theory]
    [InlineData("/catalogo/dietas", "/catalogo/dietas")]
    [InlineData("/catalogo/platos?page=2", "/catalogo/platos?page=2")]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("catalogo/dietas", "/")]
    [InlineData("https://otro-sitio.test/", "/")]
    [InlineData("//otro-sitio.test/", "/")]
    [InlineData("/\\otro-sitio.test/", "/")]
    [InlineData("/login", "/")]
    [InlineData("/login?returnUrl=%2F", "/")]
    public void DestinoSeguro_solo_acepta_rutas_locales_que_no_sean_el_login(string? returnUrl, string esperado)
        => Assert.Equal(esperado, RutasDeSesion.DestinoSeguro(returnUrl));

    [Theory]
    [InlineData("/", "/login")]
    [InlineData("/login", "/login")]
    [InlineData("/login?returnUrl=%2Fperfil", "/login")]
    [InlineData("/perfil", "/login?returnUrl=%2Fperfil")]
    public void LoginVolviendoAqui_recuerda_la_pagina_actual_salvo_la_raiz_y_el_login(string actual, string esperado)
    {
        var navegacion = Services.GetRequiredService<BunitNavigationManager>();
        navegacion.NavigateTo(actual);

        Assert.Equal(esperado, RutasDeSesion.LoginVolviendoAqui(navegacion));
    }
}
