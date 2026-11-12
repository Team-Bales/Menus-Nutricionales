using Menus.Application.Comun;

namespace Menus.Application.GestionDeMenus.RegistrarReemplazo;

/// <summary>
/// Registra que un menú pendiente se confeccionó en lugar de uno rechazado (relación <c>Origina</c>): es como queda
/// modelado que el jefe de nutrición «puede indicar la confección de otro [menú] bajo sus criterios».
/// </summary>
/// <remarks>
/// El menú de reemplazo se crea como cualquier otro, generado o manual. El vínculo se registra mientras espera
/// revisión.
/// </remarks>
/// <param name="MenuId">Identificador del menú de reemplazo, pendiente de revisión.</param>
/// <param name="MenuRechazadoId">Identificador del menú rechazado al que reemplaza.</param>
public sealed record RegistrarReemplazoCommand(Guid MenuId, Guid MenuRechazadoId) : ICommand<Unidad>;
