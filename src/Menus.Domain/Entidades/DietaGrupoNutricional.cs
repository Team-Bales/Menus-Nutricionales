using Menus.Domain.Abstractions;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Entidades;

/// <summary>
/// Registra que una dieta cubre un grupo nutricional con un tipo de cobertura (requerido o restringido).
/// </summary>
public sealed class DietaGrupoNutricional : Entity<(Guid DietaId, Guid GrupoNutricionalId)>
{
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <param name="grupoNutricionalId">Identificador del grupo nutricional.</param>
    /// <param name="tipo">Indica si el grupo es requerido o está restringido en la dieta.</param>
    public DietaGrupoNutricional(Guid dietaId, Guid grupoNutricionalId, TipoCobertura tipo)
        : base((dietaId, grupoNutricionalId))
    {
        Tipo = tipo;
    }

    /// <summary>FK → Dieta (parte de la clave compuesta).</summary>
    public Guid DietaId => Id.DietaId;

    /// <summary>FK → Grupo nutricional (parte de la clave compuesta).</summary>
    public Guid GrupoNutricionalId => Id.GrupoNutricionalId;

    /// <summary>Indica si el grupo nutricional es requerido o está restringido en la dieta.</summary>
    public TipoCobertura Tipo { get; }
}
