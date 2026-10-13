using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

public sealed class DietaGrupoNutricional : Entity<(Guid DietaId, Guid GrupoNutricionalId)>
{
    public Guid DietaId => Id.DietaId;
    public Guid GrupoNutricionalId => Id.GrupoNutricionalId;

    public DietaGrupoNutricional(Guid dietaId, Guid grupoNutricionalId)
        : base(Validar(dietaId, grupoNutricionalId))
    {
    }

    private static (Guid, Guid) Validar(Guid dietaId, Guid grupoNutricionalId)
    {
        if (dietaId == Guid.Empty)
            throw new ArgumentException("El Id de la dieta no puede estar vacío.", nameof(dietaId));

        if (grupoNutricionalId == Guid.Empty)
            throw new ArgumentException("El Id del grupo nutricional no puede estar vacío.", nameof(grupoNutricionalId));

        return (dietaId, grupoNutricionalId);
    }
}
