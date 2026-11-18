using Menus.Client.Services;
using Microsoft.Extensions.Configuration;

namespace Menus.Client.Tests.Services;

/// <summary>Pruebas de <see cref="ActorHeaderHandler"/>: la cabecera provisional mientras no exista el #74.</summary>
public sealed class ActorHeaderHandlerTests
{
    private static HttpClient Crear(string? actorId, out EcoFinal eco)
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(actorId is null ? [] : [new KeyValuePair<string, string?>("Catalogo:ActorId", actorId)])
            .Build();

        eco = new EcoFinal();
        var handler = new ActorHeaderHandler(configuracion) { InnerHandler = eco };
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
    }

    [Fact]
    public async Task Agrega_la_cabecera_cuando_el_actor_configurado_es_un_guid_valido()
    {
        var actorId = Guid.NewGuid().ToString();
        var cliente = Crear(actorId, out var eco);

        await cliente.GetAsync("api/alimentos");

        Assert.True(eco.Recibida!.Headers.TryGetValues("X-Actor-Id", out var valores));
        Assert.Equal(actorId, valores!.Single());
    }

    [Fact]
    public async Task No_agrega_la_cabecera_cuando_no_hay_actor_configurado()
    {
        var cliente = Crear(null, out var eco);

        await cliente.GetAsync("api/alimentos");

        Assert.False(eco.Recibida!.Headers.Contains("X-Actor-Id"));
    }

    [Fact]
    public async Task No_agrega_la_cabecera_cuando_el_actor_configurado_no_es_un_guid()
    {
        var cliente = Crear("no-es-un-guid", out var eco);

        await cliente.GetAsync("api/alimentos");

        Assert.False(eco.Recibida!.Headers.Contains("X-Actor-Id"));
    }

    private sealed class EcoFinal : HttpMessageHandler
    {
        public HttpRequestMessage? Recibida { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Recibida = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
