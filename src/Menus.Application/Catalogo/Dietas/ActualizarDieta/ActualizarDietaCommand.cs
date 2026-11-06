using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Dietas.ActualizarDieta;

/// <summary>Actualiza el nombre y el programa de atención de una dieta. Su cobertura y restricciones no cambian.</summary>
/// <param name="Id">Identificador de la dieta.</param>
/// <param name="Nombre">Nuevo nombre descriptivo.</param>
/// <param name="ProgramaAtencionId">Nuevo programa de atención.</param>
public sealed record ActualizarDietaCommand(Guid Id, string Nombre, Guid ProgramaAtencionId) : ICommand<Unidad>;
