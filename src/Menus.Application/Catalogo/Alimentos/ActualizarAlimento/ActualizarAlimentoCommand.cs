using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Alimentos.ActualizarAlimento;

/// <summary>
/// Cambia la clasificación de un alimento: su grupo nutricional, su tipo de preparación y su nivel calórico. El
/// nombre y el nutricionista que lo describió no cambian.
/// </summary>
/// <param name="Id">Identificador del alimento.</param>
/// <param name="GrupoNutricionalId">Nuevo grupo nutricional.</param>
/// <param name="TipoPreparacionId">Nuevo tipo de preparación, del catálogo (RD-01).</param>
/// <param name="NivelCaloricoId">Nuevo nivel calórico, del catálogo (RD-02).</param>
public sealed record ActualizarAlimentoCommand(
    Guid Id,
    Guid GrupoNutricionalId,
    Guid TipoPreparacionId,
    Guid NivelCaloricoId) : ICommand<Unidad>;
