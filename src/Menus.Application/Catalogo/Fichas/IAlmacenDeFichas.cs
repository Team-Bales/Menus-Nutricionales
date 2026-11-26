using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Fichas;

/// <summary>
/// Lectura y escritura de la ficha nutricional de un ítem, que vive en MongoDB (ADR-005, ADR-009). La implementación
/// está en <c>Menus.Infrastructure</c>.
/// </summary>
/// <remarks>
/// <para>
/// Es distinto de <see cref="IFichasNutricionales"/>, que mantiene la ficha después de un cambio ya confirmado en
/// PostgreSQL y por eso nunca lanza. Aquí la ficha es lo que se pide: si no se puede leer o escribir, el cliente
/// tiene que saberlo.
/// </para>
/// <para>
/// Sin MongoDB configurado, o si no responde, las dos operaciones lanzan <see cref="ServicioNoDisponibleException"/>
/// (503).
/// </para>
/// </remarks>
public interface IAlmacenDeFichas
{
    /// <summary>Devuelve la ficha del ítem, si tiene.</summary>
    /// <param name="clase">Clase del ítem (<c>origen.tabla</c>).</param>
    /// <param name="itemId">El ítem.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La ficha, o <see langword="null"/> si el ítem no tiene.</returns>
    /// <exception cref="ServicioNoDisponibleException">Si MongoDB no está configurado o no responde.</exception>
    Task<FichaNutricionalDto?> ObtenerAsync(ClaseDeItem clase, Guid itemId, CancellationToken ct);

    /// <summary>
    /// Guarda la ficha del ítem: la crea si no tiene y la reemplaza entera si ya tiene (a lo sumo una por ítem, RM-08).
    /// </summary>
    /// <param name="ficha">La ficha completa, ya validada por el caso de uso.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Una tarea que termina cuando la ficha quedó guardada.</returns>
    /// <exception cref="ServicioNoDisponibleException">Si MongoDB no está configurado o no responde.</exception>
    Task GuardarAsync(FichaNutricionalDto ficha, CancellationToken ct);
}
