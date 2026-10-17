using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

/// <summary>
/// Clasificación del tipo de preparación de un alimento o plato dentro de un menú.
/// Catálogo cerrado: exactamente cuatro categorías con identificadores fijos (R4).
/// </summary>
public sealed class TipoPreparacion : Entity<Guid>
{
    public static readonly TipoPreparacion Entrante =
        new(new Guid("55555555-0000-0000-0000-000000000001"), "Entrante");

    public static readonly TipoPreparacion PlatoFuerte =
        new(new Guid("55555555-0000-0000-0000-000000000002"), "Plato fuerte");

    public static readonly TipoPreparacion Postre =
        new(new Guid("55555555-0000-0000-0000-000000000003"), "Postre");

    public static readonly TipoPreparacion Bebida =
        new(new Guid("55555555-0000-0000-0000-000000000004"), "Bebida");

    public static IReadOnlyList<TipoPreparacion> Todos { get; } =
        [Entrante, PlatoFuerte, Postre, Bebida];

    private TipoPreparacion(Guid id, string nombre) : base(id) => Nombre = nombre;

    public string Nombre { get; }

    /// <summary>Indica si el identificador corresponde a uno de los tipos de preparación del catálogo (R5-08).</summary>
    /// <param name="id">Identificador que se quiere comprobar.</param>
    /// <returns><see langword="true"/> si es el de <see cref="Entrante"/>, <see cref="PlatoFuerte"/>, <see cref="Postre"/> o <see cref="Bebida"/>.</returns>
    public static bool Existe(Guid id) => Todos.Any(t => t.Id == id);
}
