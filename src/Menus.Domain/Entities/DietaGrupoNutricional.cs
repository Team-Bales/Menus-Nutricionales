namespace Menus.Domain.Entities;

public class DietaGrupoNutricional
{
    public Guid DietaId { get; private set; }
    public Guid GrupoNutricionalId { get; private set; }

    // Constructor sin parámetros para EF Core / ORM
    private DietaGrupoNutricional() { }

    public DietaGrupoNutricional(Guid dietaId, Guid grupoNutricionalId)
    {
        if (dietaId == Guid.Empty)
        {
            throw new ArgumentException("El Id de la dieta no puede estar vacío.", nameof(dietaId));
        }

        if (grupoNutricionalId == Guid.Empty)
        {
            throw new ArgumentException("El Id del grupo nutricional no puede estar vacío.", nameof(grupoNutricionalId));
        }

        DietaId = dietaId;
        GrupoNutricionalId = grupoNutricionalId;
    }
}
