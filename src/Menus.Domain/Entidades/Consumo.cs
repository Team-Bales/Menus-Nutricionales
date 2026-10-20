using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Registra la aceptación o el rechazo de un ítem del menú, alimento o plato, por un paciente ante un menú
/// asignado (relación <c>Consume</c>, con el atributo <c>Aceptación</c>).
/// Su identidad es la de la asignación más el ítem. Solo lo crea <see cref="Asignacion.RegistrarConsumo"/>.
/// </summary>
/// <remarks>
/// <b>Por qué el ítem y no el alimento.</b> Antes solo se registraba el consumo de alimentos, y un plato se
/// registraba alimento por alimento: no había forma de decir que el paciente rechazó el plato entero, y el
/// profesor lo señaló (feedback del 19/10). Con la generalización <see cref="ItemAlimentario"/>, se consume lo
/// que el menú incluye: un alimento o un plato (R11-01).
/// </remarks>
public sealed class Consumo : Entity<(Guid MenuId, Guid PacienteId, Guid ItemId)>
{
    /// <param name="menuId">Identificador del menú asignado al paciente.</param>
    /// <param name="pacienteId">Identificador del paciente que registra el consumo.</param>
    /// <param name="itemId">Identificador del alimento o plato consumido.</param>
    /// <param name="aceptacion"><see langword="true"/> si el paciente lo aceptó; de lo contrario, <see langword="false"/>.</param>
    internal Consumo(Guid menuId, Guid pacienteId, Guid itemId, bool aceptacion)
        : base((menuId, pacienteId, itemId))
    {
        Aceptacion = aceptacion;
    }

    /// <summary>FK → Menú de la asignación (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>FK → Paciente de la asignación (parte de la clave compuesta).</summary>
    public Guid PacienteId => Id.PacienteId;

    /// <summary>FK → ItemAlimentario consumido (parte de la clave compuesta).</summary>
    public Guid ItemId => Id.ItemId;

    /// <summary>Indica si el paciente aceptó el ítem.</summary>
    public bool Aceptacion { get; }
}
