using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

public sealed class DietaGrupoNutricional : Entity<(Guid DietaId, Guid GrupoNutricionalId)>
{
    public DietaGrupoNutricional(Guid dietaId, Guid grupoNutricionalId)
        : base((dietaId, grupoNutricionalId))
    {
    }

    public Guid DietaId => Id.DietaId;
    public Guid GrupoNutricionalId => Id.GrupoNutricionalId;
}
