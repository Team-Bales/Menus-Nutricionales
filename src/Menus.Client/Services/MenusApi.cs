using System.Net.Http.Json;
using System.Text.Json;
using Menus.Client.Catalogo;
using Menus.Client.GestionDeMenus;

namespace Menus.Client.Services;

/// <summary>Adaptador HTTP de los endpoints de menús (#80).</summary>
public sealed class MenusApi(HttpClient http) : IMenusApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<Guid> GenerarAutomaticamenteAsync(SolicitudDeGeneracion solicitud, CancellationToken ct = default)
    {
        var cuerpo = new
        {
            solicitud.CantidadTotalAlimentos,
            Distribucion = solicitud.Distribucion.Select(d => new
            {
                d.TipoPreparacionId,
                d.Proporcion
            }),
            solicitud.GruposNutricionalesIds
        };

        var ruta = $"api/dietas/{solicitud.DietaId}/menus/generacion-automatica";
        var creado = await EnviarAsync<MenuCreado>(HttpMethod.Post, ruta, cuerpo, ct);
        return creado.Id;
    }

    /// <inheritdoc />
    public Task<Pagina<MenuResumen>> ListarPorDietaAsync(Guid dietaId, int page, int size, CancellationToken ct = default)
        => ObtenerAsync<Pagina<MenuResumen>>($"api/menus?dietaId={dietaId}&page={page}&size={size}", ct);

    /// <inheritdoc />
    public Task<MenuDetalle> ObtenerAsync(Guid id, CancellationToken ct = default)
        => ObtenerAsync<MenuDetalle>($"api/menus/{id}", ct);

    private async Task<T> ObtenerAsync<T>(string ruta, CancellationToken ct)
    {
        using var response = await http.GetAsync(ruta, ct);
        await RespuestasDeApi.LanzarSiErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
            ?? throw new ApiException(response.StatusCode, "La API devolvió una respuesta vacía.");
    }

    private async Task<T> EnviarAsync<T>(HttpMethod method, string ruta, object cuerpo, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, ruta) { Content = JsonContent.Create(cuerpo, options: Json) };
        using var response = await http.SendAsync(request, ct);
        await RespuestasDeApi.LanzarSiErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
            ?? throw new ApiException(response.StatusCode, "La API devolvió una respuesta vacía.");
    }
}
