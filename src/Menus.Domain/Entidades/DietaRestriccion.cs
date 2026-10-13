using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

public sealed class DietaRestriccion : Entity<(Guid DietaId, Guid RestriccionAlimentariaId)>
{
    public Guid DietaId => Id.DietaId;
    public Guid RestriccionAlimentariaId => Id.RestriccionAlimentariaId;

    public DietaRestriccion(Guid dietaId, Guid restriccionAlimentariaId)
        : base(Validar(dietaId, restriccionAlimentariaId))
    {
    }

    private static (Guid, Guid) Validar(Guid dietaId, Guid restriccionAlimentariaId)
    {
        if (dietaId == Guid.Empty)
            throw new ArgumentException("El Id de la dieta no puede estar vacío.", nameof(dietaId));

        if (restriccionAlimentariaId == Guid.Empty)
            throw new ArgumentException("El Id de la restricción alimentaria no puede estar vacío.", nameof(restriccionAlimentariaId));

        return (dietaId, restriccionAlimentariaId);
    }
}
