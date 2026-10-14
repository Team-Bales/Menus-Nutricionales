using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Registra que una dieta aplica una restricción alimentaria.
/// </summary>
public sealed class DietaRestriccion : Entity<(Guid DietaId, Guid RestriccionAlimentariaId)>
{
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <param name="restriccionAlimentariaId">Identificador de la restricción alimentaria.</param>
    public DietaRestriccion(Guid dietaId, Guid restriccionAlimentariaId)
        : base((dietaId, restriccionAlimentariaId))
    {
    }

    /// <summary>FK → Dieta (parte de la clave compuesta).</summary>
    public Guid DietaId => Id.DietaId;

    /// <summary>FK → Restricción alimentaria (parte de la clave compuesta).</summary>
    public Guid RestriccionAlimentariaId => Id.RestriccionAlimentariaId;
}
