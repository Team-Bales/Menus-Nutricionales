using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

/// <summary>
/// Registra que una restricción alimentaria excluye un grupo nutricional
/// (relación <c>Excluye(RestricciónAlimentaria, GrupoNutricional)</c> del MERX).
/// Solo la crea <see cref="RestriccionAlimentaria.ExcluirGrupo"/>.
/// </summary>
public sealed class RestriccionGrupoExcluido : Entity<(Guid RestriccionAlimentariaId, Guid GrupoNutricionalId)>
{
    /// <param name="restriccionAlimentariaId">Identificador de la restricción alimentaria.</param>
    /// <param name="grupoNutricionalId">Identificador del grupo nutricional excluido.</param>
    internal RestriccionGrupoExcluido(Guid restriccionAlimentariaId, Guid grupoNutricionalId)
        : base((restriccionAlimentariaId, grupoNutricionalId)) { }

    /// <summary>FK → Restricción alimentaria (parte de la clave compuesta).</summary>
    public Guid RestriccionAlimentariaId => Id.RestriccionAlimentariaId;

    /// <summary>FK → Grupo nutricional excluido (parte de la clave compuesta).</summary>
    public Guid GrupoNutricionalId => Id.GrupoNutricionalId;
}
