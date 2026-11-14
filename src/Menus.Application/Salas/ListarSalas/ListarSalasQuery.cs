using Menus.Application.Comun;

namespace Menus.Application.Salas.ListarSalas;

/// <summary>Lista todas las salas (#72): son pocas, así que no se pagina.</summary>
public sealed record ListarSalasQuery : IQuery<IReadOnlyList<SalaDto>>;
