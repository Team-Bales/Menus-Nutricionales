using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Entidad de unión que registra un alimento incluido directamente en un menú
/// (relación <c>Incluye(Menú, Alimento)</c>). Solo la crea <see cref="Menu"/>.
/// </summary>
public sealed class MenuAlimento : Entity<(Guid MenuId, Guid AlimentoId)>
{
    /// <param name="menuId">Identificador del menú.</param>
    /// <param name="alimentoId">Identificador del alimento incluido.</param>
    internal MenuAlimento(Guid menuId, Guid alimentoId) : base((menuId, alimentoId)) { }

    /// <summary>FK → Menu (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>FK → Alimento (parte de la clave compuesta).</summary>
    public Guid AlimentoId => Id.AlimentoId;
}
