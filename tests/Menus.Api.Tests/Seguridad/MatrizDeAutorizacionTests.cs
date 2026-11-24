using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using Menus.Api.Auth;
using Menus.Api.Tests.Auth;
using Menus.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Menus.Api.Tests.Seguridad;

/// <summary>
/// Matriz de autorización por roles (#292, §13.1): cada endpoint con política de rol recibe 403 de los roles
/// que no la cumplen. Los casos se derivan de <see cref="AutorizacionEnLaApiTests.Esperado"/>, la fuente única
/// de verdad de qué política tiene cada endpoint.
/// </summary>
/// <remarks>
/// Los 403 que dependen de los datos (RS-02, RS-03, RS-05, RS-09) no se prueban aquí: necesitan base de datos y
/// viven en <c>Menus.Application.Tests</c> (<c>ManejadoresDeRevalorizacionesTests</c>,
/// <c>ManejadoresDeNutricionistasTests</c>, <c>ManejadoresDeValoracionesTests</c>). Los 401 sin token y los 403 por
/// paciente ajeno ya están en <c>AutorizacionEnLaApiTests</c>.
/// </remarks>
public sealed class MatrizDeAutorizacionTests : IClassFixture<MatrizDeAutorizacionTests.Fabrica>
{
    /// <summary>Arranca una sola API en memoria y un cliente para todos los casos de la matriz.</summary>
    public sealed class Fabrica : IAsyncLifetime
    {
        public WebApplicationFactory<Program> Api { get; private set; } = null!;

        public HttpClient Cliente { get; private set; } = null!;

        public Task InitializeAsync()
        {
            Api = ApiEnMemoria.Crear();
            Cliente = Api.CreateClient();
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            Cliente.Dispose();
            await Api.DisposeAsync();
        }
    }

    private readonly Fabrica _fabrica;

    public MatrizDeAutorizacionTests(Fabrica fabrica) => _fabrica = fabrica;

    private async Task<HttpResponseMessage> PedirAsync(string metodo, string ruta, string? token)
    {
        using var peticion = new HttpRequestMessage(new HttpMethod(metodo), ruta);
        if (token is not null)
        {
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (metodo != "GET")
        {
            peticion.Content = JsonContent.Create(new { });
        }

        return await _fabrica.Cliente.SendAsync(peticion);
    }

    [Theory]
    [MemberData(nameof(AccesosNegativos))]
    public async Task Un_rol_sin_permiso_recibe_403(string rol, string metodo, string ruta)
    {
        using var respuesta = await PedirAsync(metodo, ruta, ApiEnMemoria.Token(rol, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType!.MediaType);
        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(RespuestaDeAutorizacion.SinPermisos, problema!.Detail);
    }

    [Fact]
    public void El_mapeo_de_roles_alcanzados_es_coherente_con_el_dominio()
    {
        Assert.NotEmpty(TodosLosRoles);

        foreach (var (politica, alcanzan) in Alcanzan)
        {
            Assert.NotEmpty(RolesQueNoAlcanzan(politica));
            Assert.All(alcanzan, rol => Assert.Contains(rol, TodosLosRoles));
        }
    }

    [Fact]
    public void Toda_entrada_de_Esperado_o_no_necesita_casos_negativos_o_los_tiene()
    {
        var sinCobertura = AutorizacionEnLaApiTests.Esperado
            .Where(p => !AutorizacionEnLaApiTests.YaCubiertaAqui(p.Value))
            .Where(p => !RolesQueNoAlcanzan(p.Value).Any())
            .Select(p => $"{p.Key} ({p.Value})")
            .ToList();

        Assert.Empty(sinCobertura);
    }

    /// <summary>Casos de <c>[Theory]</c>: cada combinación de rol, método y ruta que debe recibir 403.</summary>
    public static IEnumerable<object[]> AccesosNegativos()
    {
        foreach (var (ruta, politica) in AutorizacionEnLaApiTests.Esperado)
        {
            var (metodo, path) = SepararRuta(ruta);
            foreach (var rol in RolesQueNoAlcanzan(politica))
            {
                yield return new object[] { rol, metodo, path };
            }
        }
    }

    /// <summary>Roles del dominio, leídos de <see cref="RolesUsuario"/> para no dejar ninguno fuera.</summary>
    private static readonly string[] TodosLosRoles = typeof(RolesUsuario)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(campo => campo.IsLiteral && campo.FieldType == typeof(string))
        .Select(campo => (string)campo.GetRawConstantValue()!)
        .ToArray();

    /// <summary>Roles que sí cumplen cada política de rol.</summary>
    private static readonly Dictionary<string, string[]> Alcanzan = new()
    {
        [Politicas.SoloAdmin] = [RolesUsuario.Admin],
        [Politicas.SoloNutricionista] = [RolesUsuario.Nutricionista],
        [Politicas.SoloPaciente] = [RolesUsuario.Paciente],
        [Politicas.NutricionistaOAdmin] = [RolesUsuario.Nutricionista, RolesUsuario.Admin],
    };

    private static IEnumerable<string> RolesQueNoAlcanzan(string politica)
    {
        if (!Alcanzan.TryGetValue(politica, out var alcanzan))
        {
            return [];
        }

        return TodosLosRoles.Except(alcanzan).ToArray();
    }

    private static (string Metodo, string Ruta) SepararRuta(string rutaEsperada)
    {
        var partes = rutaEsperada.Split(' ', 2);
        var ruta = "/" + Regex.Replace(partes[1], @"\{[^}]+\}", _ => Guid.NewGuid().ToString());
        return (partes[0], ruta);
    }
}
