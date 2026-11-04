using Menus.Domain.Nomenclators;

namespace Menus.Domain.ValueObjects;

/// <summary>
/// Proporción de un tipo de preparación dentro de un menú.
/// Valor en el intervalo (0, 1] (RD-11).
/// </summary>
/// <param name="TipoPreparacionId">Identificador del tipo de preparación (uno de <see cref="TipoPreparacion.Todos"/>).</param>
/// <param name="Valor">Proporción como fracción en (0, 1].</param>
public sealed record ProporcionTipoPreparacion(Guid TipoPreparacionId, decimal Valor)
{
    /// <summary>
    /// Crea una nueva proporción para un tipo de preparación.
    /// </summary>
    /// <param name="tipoPreparacionId">Identificador del tipo de preparación.</param>
    /// <param name="valor">Proporción en (0, 1].</param>
    /// <exception cref="ArgumentException">Si el tipo de preparación no existe en el catálogo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="valor"/> está fuera de (0, 1].</exception>
    public static ProporcionTipoPreparacion Crear(Guid tipoPreparacionId, decimal valor)
    {
        if (!TipoPreparacion.Existe(tipoPreparacionId))
        {
            throw new ArgumentException($"{tipoPreparacionId} no es un tipo de preparación del catálogo.", nameof(tipoPreparacionId));
        }

        if (valor <= 0m || valor > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "La proporción debe estar en el intervalo (0, 1].");
        }

        return new ProporcionTipoPreparacion(tipoPreparacionId, valor);
    }
}
