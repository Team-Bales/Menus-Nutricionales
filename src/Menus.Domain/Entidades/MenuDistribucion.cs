using Menus.Domain.Abstractions;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Entidades;

/// <summary>
/// Proporción de alimentos de un tipo de preparación dentro de un menú
/// (agregación <c>Distribución</c> / relación <c>Distribuye</c>). Es un parámetro de generación
/// del menú y solo la crea <see cref="Menu"/>.
/// </summary>
public sealed class MenuDistribucion : Entity<(Guid MenuId, TipoPreparacion TipoPreparacion)>
{
    /// <param name="menuId">Identificador del menú.</param>
    /// <param name="tipoPreparacion">Tipo de preparación al que se aplica la proporción.</param>
    /// <param name="proporcion">Proporción como fracción, en el intervalo (0, 1] (R7-16).</param>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="proporcion"/> está fuera de (0, 1].</exception>
    internal MenuDistribucion(Guid menuId, TipoPreparacion tipoPreparacion, decimal proporcion)
        : base((menuId, tipoPreparacion))
    {
        if (proporcion <= 0m || proporcion > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(proporcion), "La proporción debe estar en el intervalo (0, 1].");
        }

        Proporcion = proporcion;
    }

    /// <summary>FK → Menu (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>Tipo de preparación (parte de la clave compuesta).</summary>
    public TipoPreparacion TipoPreparacion => Id.TipoPreparacion;

    /// <summary>Proporción de alimentos de este tipo en el menú, en (0, 1].</summary>
    public decimal Proporcion { get; }
}
