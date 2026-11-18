using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Menus.Api.Auth;
using Menus.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Api.Tests.Auth;

/// <summary>
/// Pruebas de la autorización con la API entera en memoria (#74): qué política tiene cada endpoint, y cómo responden
/// los 401 y 403.
/// </summary>
public sealed class AutorizacionEnLaApiTests
{
    private const string Anonimo = "Anónimo";
    private const string Autenticado = "Autenticado";
    private const string SoloAdmin = Politicas.SoloAdmin;
    private const string SoloNutricionista = Politicas.SoloNutricionista;
    private const string NutricionistaOAdmin = Politicas.NutricionistaOAdmin;
    private const string PacientePropioOPersonal = Politicas.PacientePropioOPersonal;

    /// <summary>
    /// Lo que pide cada endpoint. Un endpoint nuevo que no esté aquí hace fallar
    /// <see cref="Cada_endpoint_exige_la_politica_acordada"/>: así nadie lo agrega sin decidir quién lo usa.
    /// </summary>
    private static readonly Dictionary<string, string> Esperado = new()
    {
        ["POST api/auth/login"] = Anonimo,
        ["POST api/auth/registro/nutricionista"] = SoloAdmin,
        ["POST api/auth/registro/paciente"] = SoloAdmin,
        ["GET api/auth/perfil"] = Autenticado,

        ["GET api/alimentos"] = Autenticado,
        ["GET api/alimentos/{id:guid}"] = Autenticado,
        ["POST api/alimentos"] = SoloNutricionista,
        ["PUT api/alimentos/{id:guid}"] = SoloNutricionista,
        ["DELETE api/alimentos/{id:guid}"] = SoloNutricionista,

        ["GET api/platos"] = Autenticado,
        ["GET api/platos/{id:guid}"] = Autenticado,
        ["POST api/platos"] = SoloNutricionista,
        ["PUT api/platos/{id:guid}"] = SoloNutricionista,
        ["DELETE api/platos/{id:guid}"] = SoloNutricionista,
        ["POST api/platos/{id:guid}/ingredientes"] = SoloNutricionista,
        ["DELETE api/platos/{id:guid}/ingredientes/{alimentoId:guid}"] = SoloNutricionista,

        ["GET api/dietas"] = Autenticado,
        ["GET api/dietas/{id:guid}"] = Autenticado,
        ["GET api/dietas/{id:guid}/disponibilidad-calorica"] = Autenticado,
        ["POST api/dietas"] = SoloAdmin,
        ["PUT api/dietas/{id:guid}"] = SoloAdmin,
        ["DELETE api/dietas/{id:guid}"] = SoloAdmin,
        ["POST api/dietas/{id:guid}/grupos"] = SoloAdmin,
        ["DELETE api/dietas/{id:guid}/grupos/{grupoId:guid}"] = SoloAdmin,
        ["POST api/dietas/{id:guid}/restricciones"] = SoloAdmin,
        ["DELETE api/dietas/{id:guid}/restricciones/{restriccionId:guid}"] = SoloAdmin,

        ["GET api/nomencladores/tipos-preparacion"] = Autenticado,
        ["GET api/nomencladores/niveles-caloricos"] = Autenticado,
        ["GET api/nomencladores/grupos-nutricionales"] = Autenticado,
        ["POST api/nomencladores/grupos-nutricionales"] = SoloAdmin,
        ["DELETE api/nomencladores/grupos-nutricionales/{id:guid}"] = SoloAdmin,
        ["GET api/nomencladores/restricciones-alimentarias"] = Autenticado,
        ["POST api/nomencladores/restricciones-alimentarias"] = SoloAdmin,
        ["DELETE api/nomencladores/restricciones-alimentarias/{id:guid}"] = SoloAdmin,
        ["GET api/nomencladores/programas-atencion"] = Autenticado,
        ["POST api/nomencladores/programas-atencion"] = SoloAdmin,
        ["DELETE api/nomencladores/programas-atencion/{id:guid}"] = SoloAdmin,
        ["GET api/nomencladores/especialidades"] = Autenticado,
        ["POST api/nomencladores/especialidades"] = SoloAdmin,
        ["DELETE api/nomencladores/especialidades/{id:guid}"] = SoloAdmin,

        ["GET api/pacientes"] = NutricionistaOAdmin,
        ["GET api/pacientes/{id:guid}"] = NutricionistaOAdmin,
        ["PUT api/pacientes/{id:guid}"] = NutricionistaOAdmin,
        ["POST api/pacientes/{id:guid}/dietas"] = NutricionistaOAdmin,
        ["DELETE api/pacientes/{id:guid}/dietas/{dietaId:guid}"] = NutricionistaOAdmin,
        ["GET api/pacientes/{id:guid}/menus"] = PacientePropioOPersonal,
        ["GET api/pacientes/{id:guid}/consumos"] = PacientePropioOPersonal,

        ["GET api/salas"] = NutricionistaOAdmin,
        ["GET api/salas/{id:guid}"] = NutricionistaOAdmin,
        ["POST api/salas"] = SoloAdmin,
        ["PUT api/salas/{id:guid}"] = SoloAdmin,
        ["DELETE api/salas/{id:guid}"] = SoloAdmin,

        ["GET api/nutricionistas"] = SoloAdmin,
        ["GET api/nutricionistas/{id:guid}"] = Autenticado,
        ["PUT api/nutricionistas/{id:guid}"] = SoloAdmin,
        ["POST api/nutricionistas/{id:guid}/dietas"] = SoloAdmin,
        ["DELETE api/nutricionistas/{id:guid}/dietas/{dietaId:guid}"] = SoloAdmin,
        ["POST api/nutricionistas/{id:guid}/rol-jefe"] = SoloAdmin,

        ["GET api/salas"] = NutricionistaOAdmin,
        ["GET api/salas/{id:guid}"] = NutricionistaOAdmin,
        ["POST api/salas"] = SoloAdmin,
        ["PUT api/salas/{id:guid}"] = SoloAdmin,
        ["DELETE api/salas/{id:guid}"] = SoloAdmin,

        ["POST api/dietas/{dietaId:guid}/menus/generacion-automatica"] = SoloNutricionista,
        ["GET api/menus"] = SoloNutricionista,
        ["GET api/menus/{id:guid}"] = SoloNutricionista,
        ["POST api/menus/{id:guid}/validar"] = SoloNutricionista,
        ["POST api/menus/{id:guid}/reemplazo"] = SoloNutricionista,
        ["POST api/menus/{id:guid}/asignaciones"] = SoloNutricionista,
        ["POST api/menus/{id:guid}/asignaciones/{pacienteId:guid}/servicios"] = SoloNutricionista,

        ["POST api/valoraciones"] = SoloNutricionista,
        ["GET api/valoraciones/{id:guid}"] = SoloNutricionista,
        ["GET api/dietas/{dietaId:guid}/valoraciones"] = SoloNutricionista,
        ["GET api/pacientes/{pacienteId:guid}/valoraciones"] = SoloNutricionista,

        ["POST api/revalorizaciones"] = Autenticado,
        ["GET api/revalorizaciones/{id:guid}"] = Autenticado,
        ["POST api/revalorizaciones/{id:guid}/resultado"] = SoloNutricionista,
        ["GET api/nutricionistas/{nutricionistaId:guid}/revalorizaciones/pendientes"] = SoloNutricionista,
    };

    private static async Task<HttpResponseMessage> PedirAsync(
        HttpMethod metodo,
        string ruta,
        string? token = null,
        object? cuerpo = null)
    {
        await using var api = ApiEnMemoria.Crear();
        using var peticion = new HttpRequestMessage(metodo, ruta);
        if (token is not null)
        {
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (metodo != HttpMethod.Get)
        {
            peticion.Content = JsonContent.Create(cuerpo ?? new { });
        }

        return await api.CreateClient().SendAsync(peticion);
    }

    /// <summary>La política efectiva de un endpoint, con el mismo vocabulario que <see cref="Esperado"/>.</summary>
    private static string PoliticaDe(Endpoint endpoint)
    {
        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return Anonimo;
        }

        var politicas = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Select(a => a.Policy)
            .OfType<string>()
            .Distinct()
            .ToList();

        // Sin política propia, rige la de reserva: usuario autenticado.
        return politicas.Count == 0 ? Autenticado : string.Join("+", politicas);
    }

    [Fact]
    public async Task Cada_endpoint_exige_la_politica_acordada()
    {
        await using var api = ApiEnMemoria.Crear();
        _ = api.Server;

        var reales = api.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
            .ToDictionary(
                e => $"{e.Metadata.GetRequiredMetadata<HttpMethodMetadata>().HttpMethods.Single()} {e.RoutePattern.RawText!.TrimStart('~', '/')}",
                PoliticaDe);

        Assert.Equal(Esperado.OrderBy(p => p.Key), reales.OrderBy(p => p.Key));
    }

    [Fact]
    public async Task Sin_token_responde_401_con_ProblemDetails_y_WWW_Authenticate()
    {
        using var respuesta = await PedirAsync(HttpMethod.Get, "/api/alimentos");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("Bearer", respuesta.Headers.WwwAuthenticate.Single().Scheme);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType!.MediaType);
        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(401, problema!.Status);
        Assert.Equal(RespuestaDeAutorizacion.NoAutenticado, problema.Detail);
    }

    [Fact]
    public async Task Un_token_firmado_con_otra_clave_responde_401()
    {
        using var respuesta = await PedirAsync(
            HttpMethod.Get, "/api/nomencladores/tipos-preparacion", ApiEnMemoria.TokenConOtraClave(RolesUsuario.Admin));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(RolesUsuario.Paciente, "/api/alimentos")]
    [InlineData(RolesUsuario.Admin, "/api/alimentos")]
    [InlineData(RolesUsuario.Nutricionista, "/api/dietas")]
    [InlineData(RolesUsuario.Paciente, "/api/nomencladores/grupos-nutricionales")]
    public async Task Un_rol_que_no_alcanza_responde_403_con_ProblemDetails(string rol, string ruta)
    {
        using var respuesta = await PedirAsync(HttpMethod.Post, ruta, ApiEnMemoria.Token(rol, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType!.MediaType);
        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(403, problema!.Status);
        Assert.Equal(RespuestaDeAutorizacion.SinPermisos, problema.Detail);
    }

    [Fact]
    public async Task Un_paciente_que_pide_los_menus_de_otro_recibe_403()
    {
        using var respuesta = await PedirAsync(
            HttpMethod.Get, $"/api/pacientes/{Guid.NewGuid()}/menus", ApiEnMemoria.Token(RolesUsuario.Paciente, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal(RespuestaDeAutorizacion.SinPermisos, (await respuesta.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail);
    }

    [Fact]
    public async Task Un_paciente_que_pide_el_historico_de_consumo_de_otro_recibe_403()
    {
        using var respuesta = await PedirAsync(
            HttpMethod.Get, $"/api/pacientes/{Guid.NewGuid()}/consumos", ApiEnMemoria.Token(RolesUsuario.Paciente, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_paciente_no_registra_servicios_y_recibe_403()
    {
        using var respuesta = await PedirAsync(
            HttpMethod.Post,
            $"/api/menus/{Guid.NewGuid()}/asignaciones/{Guid.NewGuid()}/servicios",
            ApiEnMemoria.Token(RolesUsuario.Paciente, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Sin_token_los_menus_de_un_paciente_responden_401()
    {
        using var respuesta = await PedirAsync(HttpMethod.Get, $"/api/pacientes/{Guid.NewGuid()}/menus");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(RolesUsuario.Paciente)]
    [InlineData(RolesUsuario.Nutricionista)]
    [InlineData(RolesUsuario.Admin)]
    public async Task Cualquier_usuario_autenticado_lee_los_catalogos_cerrados(string rol)
    {
        using var respuesta = await PedirAsync(HttpMethod.Get, "/api/nomencladores/niveles-caloricos", ApiEnMemoria.Token(rol));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Con_el_rol_que_alcanza_la_peticion_llega_al_caso_de_uso()
    {
        // La validación responde 422 antes de tocar la base: la autorización dejó pasar la petición.
        using var respuesta = await PedirAsync(
            HttpMethod.Post, "/api/nomencladores/grupos-nutricionales", ApiEnMemoria.Token(RolesUsuario.Admin));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_login_no_pide_token()
    {
        // Campos vacíos: la validación responde 422 sin consultar la base.
        using var respuesta = await PedirAsync(HttpMethod.Post, "/api/auth/login", cuerpo: new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task En_Development_el_documento_OpenAPI_no_pide_token()
    {
        await using var api = ApiEnMemoria.Crear("Development");

        using var respuesta = await api.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
