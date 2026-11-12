using Menus.Domain.Entidades;

namespace Menus.Application.Catalogo.Platos;

/// <summary>Un plato del banco en un listado, sin su composición pero con su tamaño (ADR-010, §2).</summary>
/// <param name="Id">Identificador del plato.</param>
/// <param name="Nombre">Nombre del plato.</param>
/// <param name="GrupoNutricionalId">Grupo nutricional al que pertenece.</param>
/// <param name="TipoPreparacionId">Tipo de preparación (RD-01).</param>
/// <param name="NivelCaloricoId">Nivel calórico (RD-02).</param>
/// <param name="NutricionistaId">Nutricionista que lo describió.</param>
/// <param name="CantidadDeIngredientes">Cuántos alimentos lo componen; al menos uno (RK-03).</param>
public sealed record PlatoDto(
    Guid Id,
    string Nombre,
    Guid GrupoNutricionalId,
    Guid TipoPreparacionId,
    Guid NivelCaloricoId,
    Guid NutricionistaId,
    int CantidadDeIngredientes)
{
    /// <summary>Copia los datos de la entidad.</summary>
    /// <param name="plato">Plato del dominio, sin sus ingredientes cargados.</param>
    /// <param name="cantidadDeIngredientes">Cuántos ingredientes tiene, contados aparte.</param>
    internal static PlatoDto Desde(Plato plato, int cantidadDeIngredientes) => new(
        plato.Id,
        plato.Nombre,
        plato.GrupoNutricionalId,
        plato.TipoPreparacionId,
        plato.NivelCaloricoId,
        plato.NutricionistaId,
        cantidadDeIngredientes);
}

/// <summary>Detalle de un plato, con su composición (#62: «incluye la lista de ingredientes con nombre y cantidad»).</summary>
/// <param name="Id">Identificador del plato.</param>
/// <param name="Nombre">Nombre del plato.</param>
/// <param name="GrupoNutricionalId">Grupo nutricional al que pertenece.</param>
/// <param name="TipoPreparacionId">Tipo de preparación (RD-01).</param>
/// <param name="NivelCaloricoId">Nivel calórico (RD-02).</param>
/// <param name="NutricionistaId">Nutricionista que lo describió.</param>
/// <param name="Ingredientes">Los alimentos que lo componen, ordenados por nombre.</param>
public sealed record PlatoDetalleDto(
    Guid Id,
    string Nombre,
    Guid GrupoNutricionalId,
    Guid TipoPreparacionId,
    Guid NivelCaloricoId,
    Guid NutricionistaId,
    IReadOnlyList<IngredienteDto> Ingredientes);

/// <summary>Un ingrediente de un plato: el alimento y su cantidad (relación <c>Compone</c>).</summary>
/// <param name="AlimentoId">Identificador del alimento.</param>
/// <param name="Nombre">Nombre del alimento.</param>
/// <param name="Cantidad">Cantidad del alimento en el plato (RD-05).</param>
public sealed record IngredienteDto(Guid AlimentoId, string Nombre, decimal Cantidad);
