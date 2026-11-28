using System.Net.Http.Json;
using Menus.Client.GestionMenus;
using Menus.Client.PanelPaciente;

namespace Menus.Client.Services;

/// <summary>Adaptador HTTP de las consultas permitidas al propio paciente.</summary>
public sealed class PanelPacienteApi(HttpClient http) : IPanelPacienteApi
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<MenuDetalle>> ListarMenusAsync(Guid pacienteId, CancellationToken ct = default)
        => ObtenerListaAsync<MenuDetalle>($"api/pacientes/{pacienteId}/menus", ct);

    /// <inheritdoc/>
    public Task<IReadOnlyList<ServicioDePaciente>> ListarConsumosAsync(Guid pacienteId, CancellationToken ct = default)
        => ObtenerListaAsync<ServicioDePaciente>($"api/pacientes/{pacienteId}/consumos", ct);

    /// <inheritdoc/>
    public Task<IReadOnlyList<ValoracionDePaciente>> ListarValoracionesAsync(Guid pacienteId, CancellationToken ct = default)
        => ObtenerListaAsync<ValoracionDePaciente>($"api/pacientes/{pacienteId}/valoraciones", ct);

    private async Task<IReadOnlyList<T>> ObtenerListaAsync<T>(string ruta, CancellationToken ct)
    {
        using var response = await http.GetAsync(ruta, ct);
        await RespuestasDeApi.LanzarSiErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<T>>(cancellationToken: ct)
            ?? throw new ApiException(response.StatusCode, "La API devolvió una lista vacía.");
    }
}
