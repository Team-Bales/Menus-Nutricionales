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
}
