using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Entidad de unión que registra un plato incluido en un menú
/// (relación <c>Incluye(Menú, Plato)</c>). Solo la crea <see cref="Menu"/>.
/// </summary>
public sealed class MenuPlato : Entity<(Guid MenuId, Guid PlatoId)>
{
    /// <param name="menuId">Identificador del menú.</param>
    /// <param name="platoId">Identificador del plato incluido.</param>
    internal MenuPlato(Guid menuId, Guid platoId) : base((menuId, platoId)) { }

    /// <summary>FK → Menu (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>FK → Plato (parte de la clave compuesta).</summary>
    public Guid PlatoId => Id.PlatoId;
}
