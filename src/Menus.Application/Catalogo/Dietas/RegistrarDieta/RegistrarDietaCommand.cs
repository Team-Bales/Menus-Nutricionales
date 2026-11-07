using Menus.Application.Comun;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Dietas.RegistrarDieta;

/// <summary>Un grupo nutricional a cubrir, con su tipo, al registrar una dieta.</summary>
/// <param name="GrupoNutricionalId">Identificador del grupo nutricional.</param>
/// <param name="Tipo">Requerido o restringido (RD-16).</param>
public sealed record GrupoACubrir(Guid GrupoNutricionalId, TipoCobertura Tipo);

/// <summary>
/// Registra una dieta nueva con su cobertura de grupos nutricionales y sus restricciones alimentarias.
/// </summary>
/// <param name="Nombre">Nombre descriptivo de la dieta.</param>
/// <param name="ProgramaAtencionId">Programa de atención al que pertenece.</param>
/// <param name="Grupos">Grupos nutricionales a cubrir; al menos uno tiene que ser requerido (RK-07).</param>
/// <param name="Restricciones">Identificadores de las restricciones alimentarias a aplicar.</param>
public sealed record RegistrarDietaCommand(
    string Nombre,
    Guid ProgramaAtencionId,
    IReadOnlyList<GrupoACubrir> Grupos,
    IReadOnlyList<Guid> Restricciones) : ICommand<IdCreadoDto>;
