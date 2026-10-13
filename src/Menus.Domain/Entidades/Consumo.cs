using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Registra la aceptación o el rechazo de un alimento por un paciente ante un menú asignado.
/// La identidad de un consumo está determinada por el menú, el paciente y el alimento.
/// </summary>
public sealed class Consumo : Entity<(Guid MenuId, Guid PacienteId, Guid AlimentoId)>
{
    /// <param name="menuId">Identificador del menú asignado al paciente.</param>
    /// <param name="pacienteId">Identificador del paciente que registra el consumo.</param>
    /// <param name="alimentoId">Identificador del alimento consumido.</param>
    /// <param name="aceptacion"><see langword="true"/> si el alimento fue aceptado; de lo contrario, <see langword="false"/>.</param>
    public Consumo(Guid menuId, Guid pacienteId, Guid alimentoId, bool aceptacion)
        : base((menuId, pacienteId, alimentoId))
    {
        Aceptacion = aceptacion;
    }

    /// <summary>FK → Menú de la asignación (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>FK → Paciente de la asignación (parte de la clave compuesta).</summary>
    public Guid PacienteId => Id.PacienteId;

    /// <summary>FK → Alimento registrado en el consumo (parte de la clave compuesta).</summary>
    public Guid AlimentoId => Id.AlimentoId;

    /// <summary>Indica si el paciente aceptó el alimento.</summary>
    public bool Aceptacion { get; }
}
