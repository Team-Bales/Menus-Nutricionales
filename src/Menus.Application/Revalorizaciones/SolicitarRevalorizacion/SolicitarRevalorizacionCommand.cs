using Menus.Application.Comun;

namespace Menus.Application.Revalorizaciones.SolicitarRevalorizacion;

/// <summary>
/// Solicita una revalorización indicando la valoración que se revisa y el nutricionista que la ejecutará.
/// </summary>
/// <param name="ValoracionRevisadaId">Identificador de la valoración nutricional que se revisa.</param>
/// <param name="NutricionistaId">Identificador del nutricionista que ejecutará la revalorización.</param>
/// <remarks>
/// Hasta que haya autenticación (#74/PR #235) no se comprueba que quien pide sea el paciente de la valoración
/// revisada; esa comprobación de autorización llega con #74 (lo anuncia el XML de <see cref="Menus.Domain.Entidades.Revalorizacion"/>).
/// </remarks>
public sealed record SolicitarRevalorizacionCommand(Guid ValoracionRevisadaId, Guid NutricionistaId) : ICommand<IdCreadoDto>;
