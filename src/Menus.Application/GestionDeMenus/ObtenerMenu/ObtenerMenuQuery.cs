using Menus.Application.Comun;

namespace Menus.Application.GestionDeMenus.ObtenerMenu;

/// <summary>Detalle de un menú, con su parametrización, sus ítems y su dictamen (#80; glosario, §3 F1 y F3).</summary>
/// <param name="Id">Identificador del menú.</param>
public sealed record ObtenerMenuQuery(Guid Id) : IQuery<MenuDetalleDto>;
