using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;

/// <summary>
/// Clave de caché de la disponibilidad calórica de una dieta, y cómo invalidarla (informe, «Consultas
/// recurrentes»; #101). La usan tanto <see cref="DisponibilidadCaloricaHandler"/> como los casos de uso que
/// cambian el banco o la dieta.
/// </summary>
internal static class CacheDeDisponibilidadCalorica
{
    /// <summary>Tiempo de vida de la clave, como respaldo de la invalidación por evento.</summary>
    public static readonly TimeSpan TiempoDeVida = TimeSpan.FromHours(1);

    /// <summary>Clave de caché de la disponibilidad calórica de una dieta.</summary>
    /// <param name="dietaId">Identificador de la dieta.</param>
    public static string Clave(Guid dietaId) => $"dieta:{dietaId}:disponibilidad-calorica";

    /// <summary>
    /// Invalida la disponibilidad calórica de una dieta puntual: la usan los casos de uso que cambian su
    /// cobertura o sus restricciones.
    /// </summary>
    /// <param name="cache">Caché a invalidar.</param>
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <param name="ct">Token de cancelación.</param>
    public static Task InvalidarAsync(ICache cache, Guid dietaId, CancellationToken ct = default)
        => cache.InvalidarAsync(Clave(dietaId), ct);

    /// <summary>
    /// Invalida la disponibilidad calórica de todas las dietas: la usan los casos de uso que cambian el banco
    /// (alta, baja o reclasificación de un alimento o un plato), porque no se sabe de antemano a cuáles afecta.
    /// </summary>
    /// <remarks>
    /// Un viaje a Redis por dieta: aceptable a la escala actual del catálogo. Si el banco crece lo suficiente
    /// para que esto pese, un índice de claves activas (un <c>SET</c> propio, o <c>SCAN</c> por patrón) lo deja
    /// en O(1) sin cambiar la firma de este método.
    /// </remarks>
    /// <param name="dietas">Repositorio de dietas, para enumerarlas.</param>
    /// <param name="cache">Caché a invalidar.</param>
    /// <param name="ct">Token de cancelación.</param>
    public static async Task InvalidarTodasAsync(IDietaRepository dietas, ICache cache, CancellationToken ct = default)
    {
        var todas = await dietas.ListAsync(ct);
        foreach (var dieta in todas)
        {
            await cache.InvalidarAsync(Clave(dieta.Id), ct);
        }
    }
}
