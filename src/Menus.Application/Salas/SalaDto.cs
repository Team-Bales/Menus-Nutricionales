using Menus.Domain.Entidades;

namespace Menus.Application.Salas;

/// <summary>Una sala del catálogo (#72).</summary>
/// <param name="Id">Identificador de la sala.</param>
/// <param name="Nombre">Nombre descriptivo de la sala.</param>
public sealed record SalaDto(Guid Id, string Nombre)
{
    internal static SalaDto Desde(Sala sala) => new(sala.Id, sala.Nombre);
}
