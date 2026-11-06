using Menus.Domain.Nomenclators;

namespace Menus.Api.Controladores;

/// <summary>Cuerpo de <c>PUT /api/dietas/{id}</c>: el nombre y el programa de atención nuevos. El id va en la ruta.</summary>
/// <param name="Nombre">Nuevo nombre descriptivo.</param>
/// <param name="ProgramaAtencionId">Nuevo programa de atención.</param>
public sealed record DatosDeDieta(string Nombre, Guid ProgramaAtencionId);

/// <summary>Cuerpo de <c>POST /api/dietas/{id}/grupos</c>: el grupo a cubrir. El id de la dieta va en la ruta.</summary>
/// <param name="GrupoNutricionalId">Identificador del grupo nutricional.</param>
/// <param name="Tipo">Requerido o restringido (RD-16).</param>
public sealed record CoberturaDeGrupo(Guid GrupoNutricionalId, TipoCobertura Tipo);

/// <summary>
/// Cuerpo de <c>POST /api/dietas/{id}/restricciones</c>: la restricción a agregar. El id de la dieta va en la ruta.
/// </summary>
/// <param name="RestriccionAlimentariaId">Identificador de la restricción alimentaria.</param>
public sealed record RestriccionDeDieta(Guid RestriccionAlimentariaId);
