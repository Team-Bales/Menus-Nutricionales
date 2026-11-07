using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Dietas.ListarDietas;

/// <summary>Lista las dietas por páginas, ordenadas por nombre, con filtro opcional por programa de atención.</summary>
/// <param name="ProgramaAtencionId">Solo las de este programa de atención.</param>
/// <param name="Page">Número de página, desde 1.</param>
/// <param name="Size">Dietas por página, entre 1 y 100.</param>
public sealed record ListarDietasQuery(
    Guid? ProgramaAtencionId = null,
    int Page = 1,
    int Size = 20) : IQuery<ResultadoPaginado<DietaResumenDto>>;
