using Menus.Domain.Entidades;

namespace Menus.Application.Catalogo.Alimentos;

/// <summary>Un alimento del banco, tal como lo ve el cliente (ADR-010, §2).</summary>
/// <param name="Id">Identificador del alimento.</param>
/// <param name="Nombre">Nombre del alimento.</param>
/// <param name="GrupoNutricionalId">Grupo nutricional al que pertenece.</param>
/// <param name="TipoPreparacionId">Tipo de preparación (RD-01).</param>
/// <param name="NivelCaloricoId">Nivel calórico (RD-02).</param>
/// <param name="NutricionistaId">Nutricionista que lo describió.</param>
public sealed record AlimentoDto(
    Guid Id,
    string Nombre,
    Guid GrupoNutricionalId,
    Guid TipoPreparacionId,
    Guid NivelCaloricoId,
    Guid NutricionistaId)
{
    /// <summary>Copia los datos de la entidad.</summary>
    /// <param name="alimento">Alimento del dominio.</param>
    internal static AlimentoDto Desde(Alimento alimento) => new(
        alimento.Id,
        alimento.Nombre,
        alimento.GrupoNutricionalId,
        alimento.TipoPreparacionId,
        alimento.NivelCaloricoId,
        alimento.NutricionistaId);
}
