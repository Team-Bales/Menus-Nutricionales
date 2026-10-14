using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

public sealed class DietaRestriccion : Entity<(Guid DietaId, Guid RestriccionAlimentariaId)>
{
    public DietaRestriccion(Guid dietaId, Guid restriccionAlimentariaId)
        : base((dietaId, restriccionAlimentariaId))
    {
    }

    public Guid DietaId => Id.DietaId;
    public Guid RestriccionAlimentariaId => Id.RestriccionAlimentariaId;
}
