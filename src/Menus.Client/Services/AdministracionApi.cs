using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Menus.Client.Administracion;
using Menus.Client.Catalogo;

namespace Menus.Client.Services;

/// <summary>Adaptador HTTP para administrar nutricionistas y salas.</summary>
public sealed class AdministracionApi(HttpClient http) : IAdministracionApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public Task<Pagina<Nutricionista>> ListarNutricionistasAsync(Guid? especialidadId, int page, int size, CancellationToken ct = default)
        => ObtenerAsync<Pagina<Nutricionista>>(RutaPaginada("api/nutricionistas", "especialidadId", especialidadId, page, size), ct);

    /// <inheritdoc />
    public Task<NutricionistaDetalle> ObtenerNutricionistaAsync(Guid id, CancellationToken ct = default)
        => ObtenerAsync<NutricionistaDetalle>($"api/nutricionistas/{id}", ct);

    /// <inheritdoc />
    public Task AutorizarDietaAsync(Guid nutricionistaId, Guid dietaId, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Post, $"api/nutricionistas/{nutricionistaId}/dietas", new { DietaId = dietaId }, ct);

    /// <inheritdoc />
    public Task RevocarDietaAsync(Guid nutricionistaId, Guid dietaId, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Delete, $"api/nutricionistas/{nutricionistaId}/dietas/{dietaId}", null, ct);

    /// <inheritdoc />
    public Task AsignarRolJefeAsync(Guid nutricionistaId, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Post, $"api/nutricionistas/{nutricionistaId}/rol-jefe", null, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<Sala>> ListarSalasAsync(CancellationToken ct = default)
        => ObtenerListaAsync<Sala>("api/salas", ct);

    /// <inheritdoc />
    public async Task<int> ContarPacientesEnSalaAsync(Guid salaId, CancellationToken ct = default)
        => (await ObtenerAsync<Pagina<Paciente>>(RutaPaginada("api/pacientes", "sala", salaId, 1, 1), ct)).Total;

    /// <inheritdoc />
    public async Task<Guid> CrearSalaAsync(SalaFormulario formulario, CancellationToken ct = default)
        => (await EnviarAsync<IdCreado>(HttpMethod.Post, "api/salas", formulario, ct)).Id;

    /// <inheritdoc />
    public Task ActualizarSalaAsync(Guid salaId, SalaFormulario formulario, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Put, $"api/salas/{salaId}", formulario, ct);

    /// <inheritdoc />
    public Task EliminarSalaAsync(Guid salaId, CancellationToken ct = default)
        => EnviarSinContenidoAsync(HttpMethod.Delete, $"api/salas/{salaId}", null, ct);

    private async Task<IReadOnlyList<T>> ObtenerListaAsync<T>(string route, CancellationToken ct)
        => await ObtenerAsync<List<T>>(route, ct);

    private async Task<T> ObtenerAsync<T>(string route, CancellationToken ct)
    {
        using var response = await http.GetAsync(route, ct);
        await RespuestasDeApi.LanzarSiErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
            ?? throw new ApiException(response.StatusCode, "La API devolvió una respuesta vacía.");
    }

    private async Task<T> EnviarAsync<T>(HttpMethod method, string route, object body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, route) { Content = JsonContent.Create(body, options: Json) };
        using var response = await http.SendAsync(request, ct);
        await RespuestasDeApi.LanzarSiErrorAsync(response, ct);
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
        await RespuestasDeApi.LanzarSiErrorAsync(response, ct);
    }

    private static string RutaPaginada(string route, string filtro, Guid? valor, int page, int size)
    {
        var query = new List<string> { $"page={page}", $"size={size}" };
        if (valor is not null)
        {
            query.Add($"{filtro}={valor}");
        }

        return $"{route}?{string.Join("&", query)}";
    }

    private sealed record IdCreado(Guid Id);
    private sealed record Paciente(Guid Id);
}
