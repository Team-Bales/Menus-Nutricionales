using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.ProgramasAtencion;

/// <summary>Registra un programa de atención nuevo en el catálogo.</summary>
/// <param name="Nombre">Nombre del programa: no vacío y único entre los programas (RD-08).</param>
public sealed record RegistrarProgramaAtencionCommand(string Nombre) : ICommand<IdCreadoDto>;
