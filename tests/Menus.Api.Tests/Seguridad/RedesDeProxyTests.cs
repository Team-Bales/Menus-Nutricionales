using Menus.Api.Tests;

namespace Menus.Api.Tests.Seguridad;

/// <summary>
/// Configuración de las redes de proxy (#294): una red válida se acepta al arrancar y una inválida detiene la API.
/// </summary>
public sealed class RedesDeProxyTests
{
    [Fact]
    public void Una_red_de_proxy_valida_se_acepta_al_arrancar()
    {
        using var api = ApiEnMemoria.Crear(ajustes: [("Seguridad:RedesDeProxy:0", "172.28.0.0/24")]);

        using var cliente = api.CreateClient();

        Assert.NotNull(cliente);
    }

    [Fact]
    public void Una_red_de_proxy_invalida_detiene_el_arranque()
    {
        using var api = ApiEnMemoria.Crear(ajustes: [("Seguridad:RedesDeProxy:0", "no-es-una-red")]);

        var error = Assert.ThrowsAny<Exception>(() => api.CreateClient());

        Assert.Contains("Seguridad:RedesDeProxy", error.ToString());
    }
}
