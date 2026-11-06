using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Platos.EliminarPlato;

/// <summary>Elimina un plato, con su composición, si no figura en ningún menú (RR-22).</summary>
/// <param name="Id">Identificador del plato.</param>
public sealed record EliminarPlatoCommand(Guid Id) : ICommand<Unidad>;
