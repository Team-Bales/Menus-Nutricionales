using Menus.Api.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Menus.Api.Tests.Cors;

/// <summary>Pruebas de la política CORS del cliente: qué admite, según la configuración.</summary>
public sealed class CorsDelClienteTests
{
    private static CorsPolicy PoliticaCon(params string[] origenes)
    {
        var valores = origenes
            .Select((origen, i) => new KeyValuePair<string, string?>($"{CorsDelCliente.Seccion}:{i}", origen));
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

        using var proveedor = new ServiceCollection()
            .AddCorsDelCliente(configuracion)
            .BuildServiceProvider();

        return proveedor.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(CorsDelCliente.Politica)!;
    }

    [Fact]
    public void Con_origenes_configurados_admite_solo_esos_con_cualquier_cabecera_y_metodo()
    {
        var politica = PoliticaCon("http://localhost:5134");

        Assert.Equal(["http://localhost:5134"], politica.Origins);
        Assert.False(politica.AllowAnyOrigin);
        Assert.True(politica.AllowAnyHeader);
        Assert.True(politica.AllowAnyMethod);
    }

    [Fact]
    public void Sin_origenes_configurados_no_admite_ninguno()
    {
        var politica = PoliticaCon();

        Assert.Empty(politica.Origins);
        Assert.False(politica.AllowAnyOrigin);
    }
}
