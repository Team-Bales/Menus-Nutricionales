using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.GruposNutricionales;

/// <summary>Registra un grupo nutricional nuevo en el catálogo.</summary>
/// <param name="Nombre">Nombre del grupo: no vacío y único entre los grupos (RD-08).</param>
public sealed record RegistrarGrupoNutricionalCommand(string Nombre) : ICommand<IdCreadoDto>;
