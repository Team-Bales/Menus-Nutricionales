using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Platos.ListarPlatos;

/// <summary>Lista los platos por páginas, ordenados por nombre, con filtros opcionales.</summary>
/// <param name="GrupoNutricionalId">Solo los de este grupo nutricional.</param>
/// <param name="TipoPreparacionId">Solo los de este tipo de preparación.</param>
/// <param name="NivelCaloricoId">Solo los de este nivel calórico.</param>
/// <param name="Page">Número de página, desde 1.</param>
/// <param name="Size">Platos por página, entre 1 y 100.</param>
public sealed record ListarPlatosQuery(
    Guid? GrupoNutricionalId = null,
    Guid? TipoPreparacionId = null,
    Guid? NivelCaloricoId = null,
    int Page = 1,
    int Size = 20) : IQuery<ResultadoPaginado<PlatoDto>>;
