using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.RestriccionesAlimentarias;

/// <summary>Lista completa de las restricciones alimentarias, ordenadas por nombre: un catálogo corto, sin paginar.</summary>
public sealed record ListarRestriccionesAlimentariasQuery : IQuery<IReadOnlyList<NomencladorDto>>;
