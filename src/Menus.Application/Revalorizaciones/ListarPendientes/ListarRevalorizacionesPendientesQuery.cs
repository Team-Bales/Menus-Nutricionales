using Menus.Application.Comun;

namespace Menus.Application.Revalorizaciones.ListarPendientes;

/// <summary>
/// Lista las revalorizaciones pendientes de un nutricionista (sin valoración generada).
/// </summary>
/// <param name="NutricionistaId">Identificador del nutricionista que ejecuta las revalorizaciones.</param>
public sealed record ListarRevalorizacionesPendientesQuery(Guid NutricionistaId) : IQuery<IReadOnlyList<RevalorizacionDto>>;

/// <summary>
/// Obtiene una revalorización por su identificador.
/// </summary>
/// <param name="Id">Identificador de la revalorización.</param>
public sealed record ObtenerRevalorizacionQuery(Guid Id) : IQuery<RevalorizacionDto>;

/// <summary>
/// DTO de una revalorización para la API.
/// </summary>
/// <param name="Id">Identificador único de la revalorización.</param>
/// <param name="FechaSolicitud">Fecha y hora en que se solicitó la revalorización (UTC).</param>
/// <param name="NutricionistaId">Identificador del nutricionista que la ejecuta.</param>
/// <param name="ValoracionRevisadaId">Identificador de la valoración que se revisa.</param>
/// <param name="ValoracionGeneradaId">Identificador de la valoración generada, o <see langword="null"/> si está pendiente.</param>
public sealed record RevalorizacionDto(
    Guid Id,
    DateTime FechaSolicitud,
    Guid NutricionistaId,
    Guid ValoracionRevisadaId,
    Guid? ValoracionGeneradaId);
