using Menus.Application.Comun;

namespace Menus.Application.Salas.ObtenerSala;

/// <summary>Detalle de una sala (#72).</summary>
/// <param name="Id">Identificador de la sala.</param>
public sealed record ObtenerSalaQuery(Guid Id) : IQuery<SalaDto>;
