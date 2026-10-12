namespace Menus.Domain.Entities;

public class DietaRestriccion
{
    public Guid DietaId { get; private set; }
    public Guid RestriccionAlimentariaId { get; private set; }

    // Constructor sin parámetros para EF Core / ORM
    private DietaRestriccion() { }

    public DietaRestriccion(Guid dietaId, Guid restriccionAlimentariaId)
    {
        if (dietaId == Guid.Empty)
            throw new ArgumentException("El Id de la dieta no puede estar vacío.", nameof(dietaId));

        if (restriccionAlimentariaId == Guid.Empty)
            throw new ArgumentException("El Id de la restricción alimentaria no puede estar vacío.", nameof(restriccionAlimentariaId));

        DietaId = dietaId;
        RestriccionAlimentariaId = restriccionAlimentariaId;
    }
}