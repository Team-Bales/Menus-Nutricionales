using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

/// <summary>
/// Clasificación del aporte calórico de un alimento o plato.
/// Catálogo cerrado: exactamente tres categorías con identificadores fijos (RD-02).
/// </summary>
public sealed class NivelCalorico : Entity<Guid>
{
    /// <summary>Aporte calórico bajo.</summary>
    public static readonly NivelCalorico Bajo =
        new(new Guid("66666666-0000-0000-0000-000000000001"), "Bajo");

    /// <summary>Aporte calórico medio.</summary>
    public static readonly NivelCalorico Medio =
        new(new Guid("66666666-0000-0000-0000-000000000002"), "Medio");

    /// <summary>Aporte calórico alto.</summary>
    public static readonly NivelCalorico Alto =
        new(new Guid("66666666-0000-0000-0000-000000000003"), "Alto");

    /// <summary>Los tres niveles del catálogo, de menor a mayor aporte.</summary>
    public static IReadOnlyList<NivelCalorico> Todos { get; } = [Bajo, Medio, Alto];

    private NivelCalorico(Guid id, string nombre) : base(id) => Nombre = nombre;

    /// <summary>Nombre del nivel, tal como se muestra.</summary>
    public string Nombre { get; }

    /// <summary>Indica si el identificador corresponde a uno de los niveles calóricos del catálogo (RD-02).</summary>
    /// <param name="id">Identificador que se quiere comprobar.</param>
    /// <returns><see langword="true"/> si es el de <see cref="Bajo"/>, <see cref="Medio"/> o <see cref="Alto"/>.</returns>
    public static bool Existe(Guid id) => Todos.Any(n => n.Id == id);
}
