using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Entidad de unión que representa la autorización de un nutricionista
/// para crear o validar menús de una dieta (relación "Autoriza").
/// </summary>
public sealed class NutricionistasDieta : Entity<(Guid NutricionistaId, Guid DietaId)>
{
    /// <param name="nutricionistaId">Identificador del nutricionista autorizado.</param>
    /// <param name="dietaId">Identificador de la dieta autorizada.</param>
    public NutricionistasDieta(Guid nutricionistaId, Guid dietaId)
        : base((nutricionistaId, dietaId))
    {
    }

    /// <summary>FK → Nutricionista (parte de la clave compuesta).</summary>
    public Guid NutricionistaId => Id.NutricionistaId;

    /// <summary>FK → Dieta (parte de la clave compuesta).</summary>
    public Guid DietaId => Id.DietaId;
}
