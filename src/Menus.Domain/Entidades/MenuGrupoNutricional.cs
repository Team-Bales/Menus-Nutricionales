using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Grupo nutricional que cubre un menú (relación <c>Cubre(Menú, GrupoNutricional)</c>).
/// Es un parámetro de generación del menú y solo la crea <see cref="Menu"/>.
/// </summary>
public sealed class MenuGrupoNutricional : Entity<(Guid MenuId, Guid GrupoNutricionalId)>
{
    /// <param name="menuId">Identificador del menú.</param>
    /// <param name="grupoNutricionalId">Identificador del grupo nutricional cubierto.</param>
    internal MenuGrupoNutricional(Guid menuId, Guid grupoNutricionalId) : base((menuId, grupoNutricionalId)) { }

    /// <summary>FK → Menu (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>FK → GrupoNutricional (parte de la clave compuesta).</summary>
    public Guid GrupoNutricionalId => Id.GrupoNutricionalId;
}
