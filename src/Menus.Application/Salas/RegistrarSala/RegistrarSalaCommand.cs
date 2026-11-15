using Menus.Application.Comun;

namespace Menus.Application.Salas.RegistrarSala;

/// <summary>Registra una sala nueva, con nombre único (#72).</summary>
/// <param name="Nombre">Nombre descriptivo de la sala.</param>
public sealed record RegistrarSalaCommand(string Nombre) : ICommand<IdCreadoDto>;
