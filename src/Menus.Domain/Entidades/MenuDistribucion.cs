using Menus.Domain.Abstractions;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Entidades;

/// <summary>
/// Proporción de alimentos de un tipo de preparación dentro de un menú
/// (relación <c>Distribuye</c>, con su atributo <c>Proporción</c>). Es un parámetro de generación
/// del menú y solo la crea <see cref="Menu"/>.
/// </summary>
public sealed class MenuDistribucion : Entity<(Guid MenuId, Guid TipoPreparacionId)>
{
    /// <param name="menuId">Identificador del menú.</param>
    /// <param name="tipoPreparacionId">
    /// FK → TipoPreparacion al que se aplica la proporción: uno de <see cref="TipoPreparacion.Todos"/> (R5-08).
    /// </param>
    /// <param name="proporcion">Proporción como fracción, en el intervalo (0, 1] (R7-16).</param>
    /// <exception cref="ArgumentException">Si el tipo de preparación no es del catálogo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="proporcion"/> está fuera de (0, 1].</exception>
    internal MenuDistribucion(Guid menuId, Guid tipoPreparacionId, decimal proporcion)
        : base((menuId, tipoPreparacionId))
    {
        if (!TipoPreparacion.Existe(tipoPreparacionId))
        {
            throw new ArgumentException($"{tipoPreparacionId} no es un tipo de preparación del catálogo.", nameof(tipoPreparacionId));
        }

        if (proporcion <= 0m || proporcion > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(proporcion), "La proporción debe estar en el intervalo (0, 1].");
        }

        Proporcion = proporcion;
    }

    /// <summary>FK → Menu (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>FK → TipoPreparacion (parte de la clave compuesta).</summary>
    public Guid TipoPreparacionId => Id.TipoPreparacionId;

    /// <summary>Proporción de alimentos de este tipo en el menú, en (0, 1].</summary>
    public decimal Proporcion { get; }
}
