using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Alimentos.EliminarAlimento;

/// <summary>Elimina un alimento que nadie usa: ni un plato (RR-04) ni un menú (RR-22).</summary>
/// <param name="Id">Identificador del alimento.</param>
public sealed record EliminarAlimentoCommand(Guid Id) : ICommand<Unidad>;
