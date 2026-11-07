using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Platos.QuitarIngrediente;

/// <summary>Quita un alimento de la composición de un plato. No se quita el único (RK-03).</summary>
/// <param name="PlatoId">Identificador del plato.</param>
/// <param name="AlimentoId">Alimento que se quita.</param>
public sealed record QuitarIngredienteCommand(Guid PlatoId, Guid AlimentoId) : ICommand<Unidad>;
