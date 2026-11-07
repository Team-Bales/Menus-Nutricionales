using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.GruposNutricionales;

/// <summary>Lista completa de los grupos nutricionales, ordenados por nombre: es un catálogo corto, sin paginar.</summary>
public sealed record ListarGruposNutricionalesQuery : IQuery<IReadOnlyList<NomencladorDto>>;
