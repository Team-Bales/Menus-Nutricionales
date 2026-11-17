using Menus.Application.Comun;

namespace Menus.Application.Nutricionistas.ListarNutricionistas;

/// <summary>Lista por páginas los nutricionistas, ordenados por nombre, con filtro opcional por especialidad (#71).</summary>
/// <param name="EspecialidadId">Si no es nulo, solo los de esta especialidad.</param>
/// <param name="Page">Número de página, desde 1.</param>
/// <param name="Size">Nutricionistas por página, entre 1 y 100.</param>
public sealed record ListarNutricionistasQuery(
    Guid? EspecialidadId = null,
    int Page = 1,
    int Size = 20) : IQuery<ResultadoPaginado<NutricionistaDto>>;
