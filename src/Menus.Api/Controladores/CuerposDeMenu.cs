namespace Menus.Api.Controladores;

/// <summary>Cuerpo de <c>POST /api/menus/{id}/validar</c>: el dictamen del jefe de nutrición. El id va en la ruta.</summary>
/// <param name="Aprobado"><see langword="true"/> para aprobar el menú; <see langword="false"/> para rechazarlo.</param>
/// <param name="Observaciones">Señalamientos del revisor, si los hay.</param>
public sealed record DictamenDeMenu(bool Aprobado, string? Observaciones);

/// <summary>
/// Cuerpo de <c>POST /api/menus/{id}/reemplazo</c>: el menú rechazado al que reemplaza. El id del menú nuevo va en la
/// ruta.
/// </summary>
/// <param name="MenuRechazadoId">Identificador del menú rechazado.</param>
public sealed record MenuReemplazado(Guid MenuRechazadoId);
