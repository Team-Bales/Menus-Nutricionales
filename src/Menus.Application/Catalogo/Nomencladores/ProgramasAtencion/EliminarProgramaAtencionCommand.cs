using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.ProgramasAtencion;

/// <summary>Elimina un programa de atención que no está en uso.</summary>
/// <param name="Id">Identificador del programa de atención.</param>
public sealed record EliminarProgramaAtencionCommand(Guid Id) : ICommand<Unidad>;
