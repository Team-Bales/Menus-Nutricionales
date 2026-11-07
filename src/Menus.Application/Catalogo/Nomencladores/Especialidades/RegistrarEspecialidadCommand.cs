using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.Especialidades;

/// <summary>Registra una especialidad nueva en el catálogo.</summary>
/// <param name="Nombre">Nombre de la especialidad: no vacío y única entre las especialidades (RD-08).</param>
public sealed record RegistrarEspecialidadCommand(string Nombre) : ICommand<IdCreadoDto>;
