namespace Menus.Api.Controladores;

/// <summary>Proporción de un tipo de preparación, dentro del cuerpo de <see cref="CuerpoDeGeneracionAutomatica"/>.</summary>
/// <param name="TipoPreparacionId">Identificador del tipo de preparación.</param>
/// <param name="Proporcion">Proporción como fracción en (0, 1].</param>
public sealed record ProporcionPorTipoDePreparacion(Guid TipoPreparacionId, decimal Proporcion);

/// <summary>
/// Cuerpo de <c>POST /api/dietas/{dietaId}/menus/generacion-automatica</c>: los parámetros de generación. La
/// dieta va en la ruta; el nutricionista creador es el actor autenticado.
/// </summary>
/// <param name="CantidadTotalAlimentos">Cantidad total de ítems del menú (&gt; 0).</param>
/// <param name="Distribucion">Proporción de ítems por tipo de preparación; debe sumar 1.</param>
/// <param name="GruposNutricionalesIds">Grupos nutricionales que debe cubrir el menú.</param>
public sealed record CuerpoDeGeneracionAutomatica(
    int CantidadTotalAlimentos,
    IReadOnlyList<ProporcionPorTipoDePreparacion> Distribucion,
    IReadOnlyList<Guid> GruposNutricionalesIds);

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

/// <summary>Cuerpo de <c>POST /api/menus/{id}/asignaciones</c>: el paciente al que se asigna. El id del menú va en la ruta.</summary>
/// <param name="PacienteId">Identificador del paciente.</param>
public sealed record AsignacionDeMenu(Guid PacienteId);

/// <summary>
/// Cuerpo de <c>POST /api/menus/{id}/asignaciones/{pacienteId}/servicios</c>: el día del servicio y la aceptación de
/// cada ítem. El menú y el paciente van en la ruta.
/// </summary>
/// <param name="Fecha">Día del servicio, en el calendario UTC (<c>aaaa-mm-dd</c>).</param>
/// <param name="Consumos">La aceptación de cada ítem; si se omite, el servicio queda sin consumo registrado.</param>
public sealed record ServicioDeMenu(DateOnly Fecha, IReadOnlyList<ConsumoDeItem>? Consumos);

/// <summary>La aceptación o el rechazo de un ítem del menú en el servicio.</summary>
/// <param name="ItemId">Identificador del alimento o plato.</param>
/// <param name="Aceptacion"><see langword="true"/> si el paciente lo aceptó; de lo contrario, <see langword="false"/>.</param>
public sealed record ConsumoDeItem(Guid ItemId, bool Aceptacion);
