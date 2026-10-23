using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

/// <summary>
/// Clasificación del aporte calórico de un alimento o plato.
/// Catálogo cerrado: exactamente tres categorías con identificadores fijos (R5).
/// </summary>
public sealed class NivelCalorico : Entity<Guid>
{
    public static readonly NivelCalorico Bajo =
        new(new Guid("66666666-0000-0000-0000-000000000001"), "Bajo");

    public static readonly NivelCalorico Medio =
        new(new Guid("66666666-0000-0000-0000-000000000002"), "Medio");

    public static readonly NivelCalorico Alto =
        new(new Guid("66666666-0000-0000-0000-000000000003"), "Alto");

    public static IReadOnlyList<NivelCalorico> Todos { get; } = [Bajo, Medio, Alto];

    private NivelCalorico(Guid id, string nombre) : base(id) => Nombre = nombre;

    public string Nombre { get; }

    /// <summary>Indica si el identificador corresponde a uno de los niveles calóricos del catálogo (R5-09).</summary>
    /// <param name="id">Identificador que se quiere comprobar.</param>
    /// <returns><see langword="true"/> si es el de <see cref="Bajo"/>, <see cref="Medio"/> o <see cref="Alto"/>.</returns>
    public static bool Existe(Guid id) => Todos.Any(n => n.Id == id);
}
