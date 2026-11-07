using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.GruposNutricionales;

/// <summary>Elimina un grupo nutricional que no está en uso.</summary>
/// <param name="Id">Identificador del grupo nutricional.</param>
public sealed record EliminarGrupoNutricionalCommand(Guid Id) : ICommand<Unidad>;
