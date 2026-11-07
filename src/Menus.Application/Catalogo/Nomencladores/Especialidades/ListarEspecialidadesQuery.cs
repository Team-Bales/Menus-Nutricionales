using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.Especialidades;

/// <summary>Lista completa de las especialidades, ordenadas por nombre: un catálogo corto, sin paginar.</summary>
public sealed record ListarEspecialidadesQuery : IQuery<IReadOnlyList<NomencladorDto>>;
