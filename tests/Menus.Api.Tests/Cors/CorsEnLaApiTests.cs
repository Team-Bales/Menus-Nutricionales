using System.Net;
using System.Net.Http.Headers;
using Menus.Domain;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Menus.Api.Tests.Cors;

/// <summary>
/// Pruebas de la política CORS con la API entera en memoria (<see cref="WebApplicationFactory{TEntryPoint}"/>): el
/// registro y el lugar de <c>UseCors</c> en <c>Program</c>, y los orígenes de <c>appsettings.Development.json</c>.
/// </summary>
/// <remarks>
/// Ninguna petición llega a la base: la cadena de conexión solo tiene que existir. Desde el #74, las peticiones llevan
/// un token, como las del cliente.
/// </remarks>
public sealed class CorsEnLaApiTests
{
    private const string OrigenDelCliente = "http://localhost:5134";
    private const string CatalogoCerrado = "/api/nomencladores/tipos-preparacion";

    private static WebApplicationFactory<Program> Api(string entorno) => ApiEnMemoria.Crear(entorno);

    private static async Task<HttpResponseMessage> PedirAsync(
        WebApplicationFactory<Program> api,
        HttpMethod metodo,
        string ruta,
        string origen,
        bool comprobacionPrevia = false,
        bool conToken = true)
    {
        using var peticion = new HttpRequestMessage(metodo, ruta);
        peticion.Headers.Add("Origin", origen);
        if (comprobacionPrevia)
        {
            // La comprobación previa del navegador nunca lleva el token: solo anuncia que lo va a mandar.
            peticion.Headers.Add("Access-Control-Request-Method", "POST");
            peticion.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        }
        else if (conToken)
        {
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiEnMemoria.Token(RolesUsuario.Nutricionista));
        }

        return await api.CreateClient().SendAsync(peticion);
    }

    private static string? OrigenAdmitido(HttpResponseMessage respuesta)
        => respuesta.Headers.TryGetValues("Access-Control-Allow-Origin", out var valores) ? valores.Single() : null;

    [Fact]
    public async Task En_Development_la_comprobacion_previa_admite_al_cliente_con_el_token()
    {
        await using var api = Api("Development");

        using var respuesta = await PedirAsync(api, HttpMethod.Options, "/api/alimentos", OrigenDelCliente, comprobacionPrevia: true);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(OrigenDelCliente, OrigenAdmitido(respuesta));
        Assert.Contains("authorization", respuesta.Headers.GetValues("Access-Control-Allow-Headers").Single());
    }

    [Fact]
    public async Task En_Development_una_respuesta_correcta_lleva_el_origen_del_cliente()
    {
        await using var api = Api("Development");

        using var respuesta = await PedirAsync(api, HttpMethod.Get, CatalogoCerrado, OrigenDelCliente);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(OrigenDelCliente, OrigenAdmitido(respuesta));
    }

    [Fact]
    public async Task En_Development_un_error_del_manejador_global_tambien_lleva_el_origen_del_cliente()
    {
        await using var api = Api("Development");

        using var respuesta = await PedirAsync(api, HttpMethod.Get, "/api/alimentos?page=0", OrigenDelCliente);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal(OrigenDelCliente, OrigenAdmitido(respuesta));
    }

    [Fact]
    public async Task En_Development_un_401_sin_token_tambien_lleva_el_origen_del_cliente()
    {
        await using var api = Api("Development");

        using var respuesta = await PedirAsync(api, HttpMethod.Get, CatalogoCerrado, OrigenDelCliente, conToken: false);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(OrigenDelCliente, OrigenAdmitido(respuesta));
    }

    [Fact]
    public async Task En_Development_otro_origen_no_se_admite()
    {
        await using var api = Api("Development");

        using var respuesta = await PedirAsync(api, HttpMethod.Get, CatalogoCerrado, "http://otro.example");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null(OrigenAdmitido(respuesta));
    }

    [Fact]
    public async Task En_Production_no_se_admite_ningun_origen()
    {
        await using var api = Api("Production");

        using var respuesta = await PedirAsync(api, HttpMethod.Get, CatalogoCerrado, OrigenDelCliente);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null(OrigenAdmitido(respuesta));
    }
}
