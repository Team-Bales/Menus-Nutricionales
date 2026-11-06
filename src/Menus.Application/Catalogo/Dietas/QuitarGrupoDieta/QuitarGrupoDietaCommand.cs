using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Dietas.QuitarGrupoDieta;

/// <summary>
/// Quita un grupo nutricional de la cobertura de una dieta. Si la dieta no lo cubre, no hace nada (idempotente).
/// </summary>
/// <param name="DietaId">Identificador de la dieta.</param>
/// <param name="GrupoNutricionalId">Identificador del grupo nutricional.</param>
public sealed record QuitarGrupoDietaCommand(Guid DietaId, Guid GrupoNutricionalId) : ICommand<Unidad>;
