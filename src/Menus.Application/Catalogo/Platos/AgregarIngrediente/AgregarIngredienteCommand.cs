using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Platos.AgregarIngrediente;

/// <summary>Agrega un alimento a la composición de un plato existente.</summary>
/// <param name="PlatoId">Identificador del plato.</param>
/// <param name="AlimentoId">Alimento simple que se agrega (RS-12).</param>
/// <param name="Cantidad">Cantidad del alimento en el plato (&gt; 0, RD-05).</param>
public sealed record AgregarIngredienteCommand(Guid PlatoId, Guid AlimentoId, decimal Cantidad) : ICommand<Unidad>;
