using System.Net.Http.Json;
using Menus.Client.GestionMenus;

namespace Menus.Client.Services;

/// <summary>Adaptador HTTP de la consulta y validación de menús.</summary>
public sealed class MenusApi(HttpClient http) : IMenusApi
{
    /// <inheritdoc/>
    public async Task<MenuDetalle> ObtenerAsync(Guid menuId, CancellationToken ct = default)
    {
        using var response = await http.GetAsync($"api/menus/{menuId}", ct);
        await RespuestasDeApi.LanzarSiErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<MenuDetalle>(cancellationToken: ct)
            ?? throw new ApiException(response.StatusCode, "La API devolvió un menú vacío.");
    }

    /// <inheritdoc/>
    public async Task ValidarAsync(Guid menuId, bool aprobado, DictamenFormulario formulario, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync(
            $"api/menus/{menuId}/validar",
            new { aprobado, observaciones = formulario.Observaciones },
            ct);
        await RespuestasDeApi.LanzarSiErrorAsync(response, ct);
    }
}
