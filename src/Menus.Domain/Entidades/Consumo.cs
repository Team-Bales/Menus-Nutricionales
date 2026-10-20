using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Registra la aceptación o el rechazo de un ítem del menú, alimento o plato, por el paciente en un servicio
/// (relación <c>Consume(Servicio, ItemAlimentario)</c>, con el atributo <c>Aceptación</c>).
/// Su identidad es la del servicio más el ítem. Solo lo crea <see cref="Menu.RegistrarConsumo"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por qué el ítem y no el alimento.</b> Antes solo se registraba el consumo de alimentos, y un plato se
/// registraba alimento por alimento: no había forma de decir que el paciente rechazó el plato entero, y el
/// profesor lo señaló (feedback del 19/10). Con la generalización <see cref="ItemAlimentario"/>, se consume lo
/// que el menú incluye: un alimento o un plato (R11-01).
/// </para>
/// <para>
/// <b>Por qué cuelga del servicio.</b> Así el mismo ítem puede registrarse en cada servicio del menú, una vez por
/// servicio (ver <see cref="Servicio"/>).
/// </para>
/// <para>
/// <b>R11-01 es declarativa.</b> Que el ítem consumido esté en el menú es la dependencia de inclusión
/// consumo[menú, ítem] ⊆ incluye[menú, ítem], así que la base la garantiza con una FK compuesta hacia
/// <see cref="MenuItem"/>, que además impide quitar del menú un ítem que ya tiene consumos. Antes hacía falta un
/// trigger, porque había que buscar el alimento también dentro de los platos incluidos. El dominio la comprueba
/// en <see cref="Menu.RegistrarConsumo"/>.
/// </para>
/// </remarks>
public sealed class Consumo : Entity<(Guid MenuId, Guid PacienteId, DateTime FechaServicio, Guid ItemId)>
{
    /// <param name="menuId">Identificador del menú servido.</param>
    /// <param name="pacienteId">Identificador del paciente que registra el consumo.</param>
    /// <param name="fechaServicio">Instante del servicio en que se registra, ya normalizado.</param>
    /// <param name="itemId">Identificador del alimento o plato consumido.</param>
    /// <param name="aceptacion"><see langword="true"/> si el paciente lo aceptó; de lo contrario, <see langword="false"/>.</param>
    internal Consumo(Guid menuId, Guid pacienteId, DateTime fechaServicio, Guid itemId, bool aceptacion)
        : base((menuId, pacienteId, fechaServicio, itemId))
    {
        Aceptacion = aceptacion;
    }

    /// <summary>FK → Menú del servicio (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>FK → Paciente del servicio (parte de la clave compuesta).</summary>
    public Guid PacienteId => Id.PacienteId;

    /// <summary>FK → Instante del servicio (parte de la clave compuesta).</summary>
    public DateTime FechaServicio => Id.FechaServicio;

    /// <summary>FK → ItemAlimentario consumido (parte de la clave compuesta).</summary>
    public Guid ItemId => Id.ItemId;

    /// <summary>Indica si el paciente aceptó el ítem.</summary>
    public bool Aceptacion { get; }
}
