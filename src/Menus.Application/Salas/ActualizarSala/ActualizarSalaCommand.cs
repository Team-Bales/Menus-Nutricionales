using Menus.Application.Comun;

namespace Menus.Application.Salas.ActualizarSala;

/// <summary>Actualiza el nombre de una sala, con nombre único (#72).</summary>
/// <param name="Id">Identificador de la sala.</param>
/// <param name="Nombre">Nuevo nombre.</param>
public sealed record ActualizarSalaCommand(Guid Id, string Nombre) : ICommand<Unidad>;
