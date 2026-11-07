using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Dietas.QuitarRestriccionDieta;

/// <summary>Quita una restricción alimentaria de una dieta. Si la dieta no la tiene, no hace nada (idempotente).</summary>
/// <param name="DietaId">Identificador de la dieta.</param>
/// <param name="RestriccionAlimentariaId">Identificador de la restricción alimentaria.</param>
public sealed record QuitarRestriccionDietaCommand(Guid DietaId, Guid RestriccionAlimentariaId) : ICommand<Unidad>;
