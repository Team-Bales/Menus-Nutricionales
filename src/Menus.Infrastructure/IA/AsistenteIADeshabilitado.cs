using Menus.Application.IA;

namespace Menus.Infrastructure.IA;

/// <summary>
/// Implementación de <see cref="IAsistenteIA"/> que se registra cuando <see cref="IAOptions.Habilitada"/> es
/// <see langword="false"/> (#323). No crea ningún cliente HTTP y siempre informa de que la IA no está disponible.
/// </summary>
public sealed class AsistenteIADeshabilitado : IAsistenteIA
{
    /// <inheritdoc/>
    public Task<string> CompletarAsync(PromptIA prompt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        throw new AsistenteIANoDisponibleException("La integración de IA está deshabilitada.");
    }
}
