namespace Menus.Application.GestionDeMenus;

/// <summary>La aceptación o el rechazo de un ítem del menú en un servicio (relación <c>Consume</c>, #85).</summary>
/// <param name="ItemId">Identificador del alimento o plato.</param>
/// <param name="Nombre">Nombre del alimento o plato.</param>
/// <param name="Aceptacion"><see langword="true"/> si el paciente lo aceptó; de lo contrario, <see langword="false"/>.</param>
public sealed record ConsumoDto(Guid ItemId, string Nombre, bool Aceptacion);

/// <summary>Un día en que se le sirvió un menú al paciente, con sus consumos (entidad débil <c>Servicio</c>, #85).</summary>
/// <param name="MenuId">Identificador del menú servido.</param>
/// <param name="DietaId">Dieta del menú.</param>
/// <param name="Fecha">Día del servicio, en el calendario UTC.</param>
/// <param name="Consumos">Los consumos registrados en el servicio, por nombre del ítem; vacía si no hay.</param>
public sealed record ServicioDto(Guid MenuId, Guid DietaId, DateOnly Fecha, IReadOnlyList<ConsumoDto> Consumos);
