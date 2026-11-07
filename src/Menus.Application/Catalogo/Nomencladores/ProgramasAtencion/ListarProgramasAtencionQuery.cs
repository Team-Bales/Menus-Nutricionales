using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.ProgramasAtencion;

/// <summary>Lista completa de los programas de atención, ordenados por nombre: un catálogo corto, sin paginar.</summary>
public sealed record ListarProgramasAtencionQuery : IQuery<IReadOnlyList<NomencladorDto>>;
