using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.Especialidades;

/// <summary>Elimina una especialidad que no está en uso.</summary>
/// <param name="Id">Identificador de la especialidad.</param>
public sealed record EliminarEspecialidadCommand(Guid Id) : ICommand<Unidad>;
