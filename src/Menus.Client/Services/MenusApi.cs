using System.Net.Http.Json;
using System.Text.Json;
using Menus.Client.Catalogo;
using Menus.Client.MenusGenerados;

namespace Menus.Client.Services;

/// <summary>Adaptador HTTP de los endpoints de menús.</summary>
public sealed class MenusApi(HttpClient http) : IMenusApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public Task<Pagina<Menu>> ListarMenusAsync(Guid dietaId, string? estado, int page, int size, CancellationToken ct = default)
    {
        var query = new List<string> { $"dietaId={dietaId}", $"page={page}", $"size={size}" };
        if (!string.IsNullOrWhiteSpace(estado))
        {
            query.Add($"estado={Uri.EscapeDataString(estado)}");
        }

        return ObtenerAsync<Pagina<Menu>>($"api/menus?{string.Join("&", query)}", ct);
    }

    /// <inheritdoc />
    public Task<MenuDetalle> ObtenerMenuAsync(Guid id, CancellationToken ct = default)
        => ObtenerAsync<MenuDetalle>($"api/menus/{id}", ct);

    /// <inheritdoc />
    public async Task<Guid> GenerarMenuAsync(GeneracionDeMenu generacion, CancellationToken ct = default)
        => (await EnviarAsync<IdCreado>(
            HttpMethod.Post,
            $"api/dietas/{generacion.DietaId}/menus/generacion-automatica",
            new
            {
                generacion.CantidadTotalAlimentos,
                generacion.Distribucion,
                generacion.GruposNutricionalesIds,
            },
            ct)).Id;

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

    private sealed record IdCreado(Guid Id);
}
