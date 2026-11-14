using Menus.Application.Comun;

namespace Menus.Application.Revalorizaciones.RegistrarResultadoRevalorizacion;

/// <summary>
/// Registra el resultado de una revalorización: la valoración nutricional generada por el nutricionista ejecutor.
/// </summary>
/// <param name="RevalorizacionId">Identificador de la revalorización que se completa.</param>
/// <param name="ResultadoNutricionalId">Identificador de la categoría de resultado nutricional (RR-17).</param>
public sealed record RegistrarResultadoRevalorizacionCommand(Guid RevalorizacionId, Guid ResultadoNutricionalId) : ICommand<IdCreadoDto>;
