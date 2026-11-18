using Menus.Application.Comun;

namespace Menus.Application.GestionDeMenus.RegistrarServicio;

/// <summary>
/// Registra que un menú asignado se le sirvió al paciente un día (entidad débil <c>Servicio</c>) y, en ese servicio,
/// si aceptó o rechazó cada ítem (relación <c>Consume</c>), todo en una transacción (#85; RF-14).
/// </summary>
/// <param name="MenuId">Identificador del menú servido.</param>
/// <param name="PacienteId">Identificador del paciente al que se sirvió.</param>
/// <param name="Fecha">Día del servicio, en el calendario UTC.</param>
/// <param name="Consumos">
/// La aceptación de cada ítem en ese servicio. Puede estar vacía: un servicio puede no tener consumo registrado
/// (informe, límites del modelo).
/// </param>
public sealed record RegistrarServicioCommand(
    Guid MenuId,
    Guid PacienteId,
    DateOnly Fecha,
    IReadOnlyList<ConsumoDelServicio> Consumos) : ICommand<Unidad>;

/// <summary>La aceptación o el rechazo de un ítem del menú, alimento o plato, en un servicio.</summary>
/// <param name="ItemId">Identificador del alimento o plato.</param>
/// <param name="Aceptacion"><see langword="true"/> si el paciente lo aceptó; de lo contrario, <see langword="false"/>.</param>
public sealed record ConsumoDelServicio(Guid ItemId, bool Aceptacion);
