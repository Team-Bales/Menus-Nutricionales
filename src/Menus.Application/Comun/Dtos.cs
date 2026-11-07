namespace Menus.Application.Comun;

/// <summary>
/// Página de un listado, con el sobre que piden los issues (#61 a #64): <c>{ data, total, page, size }</c>
/// (ADR-010, §2).
/// </summary>
/// <typeparam name="T">DTO de cada elemento.</typeparam>
/// <param name="Data">Elementos de la página.</param>
/// <param name="Total">Total de elementos del listado completo, sin paginar.</param>
/// <param name="Page">Número de página, desde 1.</param>
/// <param name="Size">Tamaño de página pedido.</param>
public sealed record ResultadoPaginado<T>(IReadOnlyList<T> Data, int Total, int Page, int Size);

/// <summary>
/// Resultado de un comando que crea un recurso: su identificador, para el <c>Location</c> del 201.
/// </summary>
/// <param name="Id">Identificador del recurso creado.</param>
public sealed record IdCreadoDto(Guid Id);
