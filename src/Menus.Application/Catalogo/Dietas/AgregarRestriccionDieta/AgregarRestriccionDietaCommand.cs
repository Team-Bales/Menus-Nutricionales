using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Dietas.AgregarRestriccionDieta;

/// <summary>
/// Agrega una restricción alimentaria a una dieta. Si la dieta ya la tiene, no hace nada (idempotente): no falla
/// ni la duplica.
/// </summary>
/// <param name="DietaId">Identificador de la dieta.</param>
/// <param name="RestriccionAlimentariaId">Identificador de la restricción alimentaria.</param>
public sealed record AgregarRestriccionDietaCommand(Guid DietaId, Guid RestriccionAlimentariaId) : ICommand<Unidad>;
