using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Dietas.EliminarDieta;

/// <summary>Elimina una dieta que no tiene pacientes inscritos ni menús.</summary>
/// <param name="Id">Identificador de la dieta.</param>
public sealed record EliminarDietaCommand(Guid Id) : ICommand<Unidad>;
