using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Entidad de unión que registra un ítem del banco, alimento o plato, incluido en un menú
/// (relación <c>Incluye(Menú, ItemAlimentario)</c>). Solo la crea <see cref="Menu"/>.
/// </summary>
/// <remarks>
/// Reemplaza a las dos inclusiones paralelas que había (una para alimentos y otra para platos): con la
/// generalización <see cref="ItemAlimentario"/>, la relación existe una sola vez. Un ítem aparece a lo sumo una
/// vez en un mismo menú (RC-15). Los consumos la referencian (RX-01), así que no se quita del menú un ítem que
/// ya tiene consumos registrados.
/// </remarks>
public sealed class MenuItem : Entity<(Guid MenuId, Guid ItemId)>
{
    /// <param name="menuId">Identificador del menú.</param>
    /// <param name="itemId">Identificador del alimento o plato incluido.</param>
    internal MenuItem(Guid menuId, Guid itemId) : base((menuId, itemId)) { }

    /// <summary>FK → Menu (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>FK → ItemAlimentario (parte de la clave compuesta).</summary>
    public Guid ItemId => Id.ItemId;
}
