using Menus.Application.Comun;

namespace Menus.Application.Tests.Comun;

/// <summary>
/// Caché en memoria para probar los manejadores sin Redis (ADR-010, §9). <see cref="Disponible"/> simula una
/// falla de Redis (ADR-005): mientras es <see langword="false"/>, <see cref="ObtenerAsync{T}"/> siempre
/// devuelve <see langword="null"/> y <see cref="GuardarAsync{T}"/>/<see cref="InvalidarAsync"/> no hacen nada.
/// </summary>
internal sealed class CacheFalsa : ICache
{
    private readonly Dictionary<string, object> _valores = [];

    public bool Disponible { get; set; } = true;

    public int Lecturas { get; private set; }

    public int Escrituras { get; private set; }

    public Task<T?> ObtenerAsync<T>(string clave, CancellationToken ct = default) where T : class
    {
        Lecturas++;
        if (!Disponible)
        {
            return Task.FromResult<T?>(null);
        }

        return Task.FromResult(_valores.TryGetValue(clave, out var valor) ? (T)valor : null);
    }

    public Task GuardarAsync<T>(string clave, T valor, TimeSpan tiempoDeVida, CancellationToken ct = default) where T : class
    {
        Escrituras++;
        if (Disponible)
        {
            _valores[clave] = valor;
        }

        return Task.CompletedTask;
    }

    public Task InvalidarAsync(string clave, CancellationToken ct = default)
    {
        if (Disponible)
        {
            _valores.Remove(clave);
        }

        return Task.CompletedTask;
    }
}
