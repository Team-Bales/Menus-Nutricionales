using Menus.Application.Comun;

namespace Menus.Application.GestionDeMenus.ValidarMenu;

/// <summary>
/// Registra el dictamen del jefe de nutrición sobre un menú pendiente: lo aprueba, y el menú pasa a ser final, o lo
/// rechaza, y el menú queda registrado como rechazado (relación <c>Valida</c>). El primer dictamen cierra el menú
/// (RC-12, RS-15).
/// </summary>
/// <remarks>
/// Si el jefe pide confeccionar otro menú, sus criterios van en <paramref name="Observaciones"/>, y el menú nuevo se
/// vincula al rechazado con <c>RegistrarReemplazoCommand</c>.
/// </remarks>
/// <param name="MenuId">Identificador del menú.</param>
/// <param name="Aprobado"><see langword="true"/> para aprobarlo; <see langword="false"/> para rechazarlo.</param>
/// <param name="Observaciones">Señalamientos del revisor, si los hay.</param>
public sealed record ValidarMenuCommand(Guid MenuId, bool Aprobado, string? Observaciones = null) : ICommand<Unidad>;
