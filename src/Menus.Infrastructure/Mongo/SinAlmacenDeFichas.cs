using Menus.Application.Catalogo.Fichas;
using Menus.Application.Comun;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Implementación de <see cref="IAlmacenDeFichas"/> cuando no hay MongoDB configurado: la API arranca igual, pero las
/// fichas no se pueden leer ni escribir, y se responde 503 (#283).
/// </summary>
/// <remarks>
/// No es <see langword="null"/> ni «sin ficha»: devolver 404 escondería que lo que falta es el servicio, no la ficha.
/// </remarks>
public sealed class SinAlmacenDeFichas : IAlmacenDeFichas
{
    /// <summary>Mensaje del 503.</summary>
    public const string Mensaje = "La ficha nutricional no está disponible: MongoDB no está configurado.";

    /// <inheritdoc />
    public Task<FichaNutricionalDto?> ObtenerAsync(ClaseDeItem clase, Guid itemId, CancellationToken ct)
        => throw new ServicioNoDisponibleException(Mensaje);

    /// <inheritdoc />
    public Task GuardarAsync(FichaNutricionalDto ficha, CancellationToken ct)
        => throw new ServicioNoDisponibleException(Mensaje);
}
