using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Menus.Client.Catalogo;

namespace Menus.Client.Services;

/// <summary>Adaptador HTTP de los endpoints de alimentos, platos y nomencladores.</summary>
public sealed class CatalogoApi(HttpClient http) : ICatalogoApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<Nomenclador>> ListarGruposAsync(CancellationToken ct = default)
        => ObtenerListaAsync("api/nomencladores/grupos-nutricionales", ct);

    public Task<IReadOnlyList<Nomenclador>> ListarRestriccionesAsync(CancellationToken ct = default)
        => ObtenerListaAsync("api/nomencladores/restricciones-alimentarias", ct);

    public Task<IReadOnlyList<Nomenclador>> ListarProgramasAsync(CancellationToken ct = default)
        => ObtenerListaAsync("api/nomencladores/programas-atencion", ct);

    public Task<IReadOnlyList<Nomenclador>> ListarEspecialidadesAsync(CancellationToken ct = default)
        => ObtenerListaAsync("api/nomencladores/especialidades", ct);

    public Task<IReadOnlyList<Nomenclador>> ListarTiposPreparacionAsync(CancellationToken ct = default)
        => ObtenerListaAsync("api/nomencladores/tipos-preparacion", ct);

    public Task<IReadOnlyList<Nomenclador>> ListarNivelesCaloricosAsync(CancellationToken ct = default)
        => ObtenerListaAsync("api/nomencladores/niveles-caloricos", ct);

    public async Task<Guid> CrearNomencladorAsync(TipoDeNomenclador tipo, NomencladorFormulario formulario, CancellationToken ct = default)
        => (await EnviarAsync<IdCreado>(HttpMethod.Post, RutaNomenclador(tipo), formulario, ct)).Id;

    public Task EliminarNomencladorAsync(TipoDeNomenclador tipo, Guid id, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Delete, $"{RutaNomenclador(tipo)}/{id}", null, ct);

    public Task<Pagina<Dieta>> ListarDietasAsync(Guid? programa, int page, int size, CancellationToken ct = default)
        => ObtenerAsync<Pagina<Dieta>>(RutaPaginada("api/dietas", "programa", programa, page, size), ct);

    public Task<DietaDetalle> ObtenerDietaAsync(Guid id, CancellationToken ct = default)
        => ObtenerAsync<DietaDetalle>($"api/dietas/{id}", ct);

    public Task AgregarGrupoDietaAsync(Guid dietaId, GrupoCubiertoFormulario grupo, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Post, $"api/dietas/{dietaId}/grupos", grupo, ct);

    public Task QuitarGrupoDietaAsync(Guid dietaId, Guid grupoId, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Delete, $"api/dietas/{dietaId}/grupos/{grupoId}", null, ct);

    public Task AgregarRestriccionDietaAsync(Guid dietaId, Guid restriccionId, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Post, $"api/dietas/{dietaId}/restricciones", new { RestriccionAlimentariaId = restriccionId }, ct);

    public Task QuitarRestriccionDietaAsync(Guid dietaId, Guid restriccionId, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Delete, $"api/dietas/{dietaId}/restricciones/{restriccionId}", null, ct);

    public Task<Pagina<Alimento>> ListarAlimentosAsync(Guid? grupo, Guid? tipo, Guid? nivel, int page, int size, CancellationToken ct = default)
        => ObtenerAsync<Pagina<Alimento>>(RutaPaginada("api/alimentos", grupo, tipo, nivel, page, size), ct);

    public async Task<IReadOnlyList<Alimento>> ListarTodosLosAlimentosAsync(CancellationToken ct = default)
    {
        const int size = 100;
        var firstPage = await ListarAlimentosAsync(null, null, null, 1, size, ct);
        var alimentos = firstPage.Data.ToList();
        var pages = (int)Math.Ceiling(firstPage.Total / (double)size);

        for (var page = 2; page <= pages; page++)
        {
            alimentos.AddRange((await ListarAlimentosAsync(null, null, null, page, size, ct)).Data);
        }

        return alimentos;
    }

    public Task<Alimento> ObtenerAlimentoAsync(Guid id, CancellationToken ct = default)
        => ObtenerAsync<Alimento>($"api/alimentos/{id}", ct);

    public async Task<Guid> CrearAlimentoAsync(ClasificacionFormulario alimento, CancellationToken ct = default)
        => (await EnviarAsync<IdCreado>(HttpMethod.Post, "api/alimentos", alimento, ct)).Id;

    public Task ActualizarAlimentoAsync(Guid id, ClasificacionFormulario alimento, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Put, $"api/alimentos/{id}", Clasificacion(alimento), ct);

    public Task EliminarAlimentoAsync(Guid id, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Delete, $"api/alimentos/{id}", null, ct);

    public Task<Pagina<Plato>> ListarPlatosAsync(Guid? grupo, Guid? tipo, Guid? nivel, int page, int size, CancellationToken ct = default)
        => ObtenerAsync<Pagina<Plato>>(RutaPaginada("api/platos", grupo, tipo, nivel, page, size), ct);

    public Task<PlatoDetalle> ObtenerPlatoAsync(Guid id, CancellationToken ct = default)
        => ObtenerAsync<PlatoDetalle>($"api/platos/{id}", ct);

    public async Task<Guid> CrearPlatoAsync(ClasificacionFormulario plato, IReadOnlyList<IngredienteFormulario> ingredientes, CancellationToken ct = default)
    {
        var request = new
        {
            plato.Nombre,
            plato.GrupoNutricionalId,
            plato.TipoPreparacionId,
            plato.NivelCaloricoId,
            Ingredientes = ingredientes.Select(i => new { i.AlimentoId, i.Cantidad })
        };

        return (await EnviarAsync<IdCreado>(HttpMethod.Post, "api/platos", request, ct)).Id;
    }

    public Task ActualizarPlatoAsync(Guid id, ClasificacionFormulario plato, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Put, $"api/platos/{id}", Clasificacion(plato), ct);

    public Task EliminarPlatoAsync(Guid id, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Delete, $"api/platos/{id}", null, ct);

    public Task AgregarIngredienteAsync(Guid platoId, IngredienteFormulario ingrediente, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Post, $"api/platos/{platoId}/ingredientes", ingrediente, ct);

    public Task QuitarIngredienteAsync(Guid platoId, Guid alimentoId, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Delete, $"api/platos/{platoId}/ingredientes/{alimentoId}", null, ct);

    private async Task<IReadOnlyList<Nomenclador>> ObtenerListaAsync(string route, CancellationToken ct)
        => await ObtenerAsync<List<Nomenclador>>(route, ct);

    private async Task<T> ObtenerAsync<T>(string route, CancellationToken ct)
    {
        using var response = await http.GetAsync(route, ct);
        await LanzarSiErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
            ?? throw new ApiException(response.StatusCode, "La API devolvió una respuesta vacía.");
    }

    private async Task<T> EnviarAsync<T>(HttpMethod method, string route, object body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, route) { Content = JsonContent.Create(body, options: Json) };
        using var response = await http.SendAsync(request, ct);
        await LanzarSiErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
            ?? throw new ApiException(response.StatusCode, "La API devolvió una respuesta vacía.");
    }

    private async Task EnviarSinContenidoAsync(HttpMethod method, string route, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, route);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        using var response = await http.SendAsync(request, ct);
        await LanzarSiErrorAsync(response, ct);
    }

    private static async Task LanzarSiErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = $"La API respondió {(int)response.StatusCode}.";
        var content = await response.Content.ReadAsStringAsync(ct);
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.TryGetProperty("detail", out var detail) && !string.IsNullOrWhiteSpace(detail.GetString()))
                {
                    message = detail.GetString()!;
                }
            }
            catch (JsonException)
            {
                // La respuesta no usa ProblemDetails; se conserva un mensaje genérico y seguro.
            }
        }

        throw new ApiException(response.StatusCode, message);
    }

    private static object Clasificacion(ClasificacionFormulario item) => new
    {
        item.GrupoNutricionalId,
        item.TipoPreparacionId,
        item.NivelCaloricoId
    };

    private static string RutaNomenclador(TipoDeNomenclador tipo) => tipo switch
    {
        TipoDeNomenclador.GruposNutricionales => "api/nomencladores/grupos-nutricionales",
        TipoDeNomenclador.RestriccionesAlimentarias => "api/nomencladores/restricciones-alimentarias",
        TipoDeNomenclador.ProgramasAtencion => "api/nomencladores/programas-atencion",
        TipoDeNomenclador.Especialidades => "api/nomencladores/especialidades",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "El nomenclador no es administrable."),
    };

    private static string RutaPaginada(string route, Guid? grupo, Guid? tipo, Guid? nivel, int page, int size)
    {
        var query = new List<string> { $"page={page}", $"size={size}" };
        if (grupo is not null)
        {
            query.Add($"grupo={grupo}");
        }

        if (tipo is not null)
        {
            query.Add($"tipo={tipo}");
        }

        if (nivel is not null)
        {
            query.Add($"nivel={nivel}");
        }
        return $"{route}?{string.Join("&", query)}";
    }

    private static string RutaPaginada(string route, string nombreDeFiltro, Guid? filtro, int page, int size)
    {
        var query = new List<string> { $"page={page}", $"size={size}" };
        if (filtro is not null)
        {
            query.Add($"{nombreDeFiltro}={filtro}");
        }

        return $"{route}?{string.Join("&", query)}";
    }

    private sealed record IdCreado(Guid Id);
}

/// <summary>Error de una respuesta HTTP que puede mostrarse de forma legible en la UI.</summary>
public sealed class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
