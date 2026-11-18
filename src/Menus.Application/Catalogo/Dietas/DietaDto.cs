using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Dietas;

/// <summary>Un grupo nutricional que cubre una dieta, con su tipo de cobertura (ADR-010, §2).</summary>
/// <param name="GrupoNutricionalId">Identificador del grupo nutricional.</param>
/// <param name="Tipo">Requerido o restringido (RD-16).</param>
public sealed record GrupoCubiertoDto(Guid GrupoNutricionalId, TipoCobertura Tipo);

/// <summary>Una dieta, sin su cobertura ni sus restricciones expandidas: la forma que usa el listado.</summary>
/// <param name="Id">Identificador de la dieta.</param>
/// <param name="Nombre">Nombre descriptivo.</param>
/// <param name="ProgramaAtencionId">Programa de atención al que pertenece.</param>
/// <param name="CantidadDeGrupos">Cuántos grupos nutricionales cubre, para el listado del catálogo (#66).</param>
/// <param name="CantidadDeRestricciones">Cuántas restricciones alimentarias aplica, para el listado del catálogo (#66).</param>
public sealed record DietaResumenDto(
    Guid Id,
    string Nombre,
    Guid ProgramaAtencionId,
    int CantidadDeGrupos,
    int CantidadDeRestricciones)
{
    /// <summary>Copia los datos de la entidad.</summary>
    /// <param name="dieta">Dieta del dominio.</param>
    /// <param name="conteos">Cantidades de relaciones de la dieta, contadas por lote.</param>
    internal static DietaResumenDto Desde(Dieta dieta, ConteosDeDieta conteos) => new(
        dieta.Id,
        dieta.Nombre,
        dieta.ProgramaAtencionId,
        conteos.CantidadDeGrupos,
        conteos.CantidadDeRestricciones);
}

/// <summary>Una dieta con su cobertura y sus restricciones expandidas: la forma que usa el detalle.</summary>
/// <param name="Id">Identificador de la dieta.</param>
/// <param name="Nombre">Nombre descriptivo.</param>
/// <param name="ProgramaAtencionId">Programa de atención al que pertenece.</param>
/// <param name="Grupos">Grupos nutricionales que cubre, con su tipo.</param>
/// <param name="Restricciones">Identificadores de las restricciones alimentarias que aplica.</param>
public sealed record DietaDto(
    Guid Id,
    string Nombre,
    Guid ProgramaAtencionId,
    IReadOnlyList<GrupoCubiertoDto> Grupos,
    IReadOnlyList<Guid> Restricciones)
{
    /// <summary>Copia los datos de la entidad, con sus colecciones ya cargadas.</summary>
    /// <param name="dieta">Dieta del dominio, con <see cref="Dieta.GruposNutricionales"/> y <see cref="Dieta.Restricciones"/> cargados.</param>
    internal static DietaDto Desde(Dieta dieta) => new(
        dieta.Id,
        dieta.Nombre,
        dieta.ProgramaAtencionId,
        [.. dieta.GruposNutricionales.Select(g => new GrupoCubiertoDto(g.GrupoNutricionalId, g.Tipo))],
        [.. dieta.Restricciones.Select(r => r.RestriccionAlimentariaId)]);
}
