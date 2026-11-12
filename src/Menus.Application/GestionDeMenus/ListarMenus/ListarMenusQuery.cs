using Menus.Application.Comun;
using Menus.Domain.Entidades;

namespace Menus.Application.GestionDeMenus.ListarMenus;

/// <summary>
/// Lista por páginas los menús de una dieta, del más reciente al más antiguo, los haya creado cualquiera de los
/// nutricionistas autorizados para ella (#80; glosario, §3 F1).
/// </summary>
/// <param name="DietaId">Dieta de los menús. Es obligatoria.</param>
/// <param name="Estado">Si no es nulo, solo los menús en ese estado.</param>
/// <param name="Page">Número de página, desde 1.</param>
/// <param name="Size">Menús por página, entre 1 y 100.</param>
public sealed record ListarMenusQuery(
    Guid DietaId,
    EstadoDeMenu? Estado = null,
    int Page = 1,
    int Size = 20) : IQuery<ResultadoPaginado<MenuDto>>;
