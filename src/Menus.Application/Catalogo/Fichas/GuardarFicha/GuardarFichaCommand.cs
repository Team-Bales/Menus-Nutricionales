using System.Text.Json.Nodes;
using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Fichas.GuardarFicha;

/// <summary>
/// Crea o reemplaza la ficha nutricional de un alimento o un plato (#283). <c>origen</c>, <c>tipo</c> y
/// <c>actualizadoEn</c> no se mandan: los fija el servidor a partir del ítem (RM-01 a RM-03).
/// </summary>
/// <param name="Clase">Clase del ítem, según la ruta.</param>
/// <param name="ItemId">El alimento o el plato.</param>
/// <param name="PorcionReferencia">Porción de referencia.</param>
/// <param name="Macronutrientes">Energía y macronutrientes.</param>
/// <param name="Micronutrientes">Vitaminas, minerales u otros.</param>
/// <param name="Alergenos">Alérgenos presentes; vacío es «sin alérgenos» y <see langword="null"/>, «no registrado».</param>
/// <param name="ModoPreparacion">Pasos, tiempos y técnica.</param>
/// <param name="Ingredientes">Desglose por ingrediente; solo para un plato.</param>
public sealed record GuardarFichaCommand(
    ClaseDeItem Clase,
    Guid ItemId,
    PorcionDeReferenciaDto? PorcionReferencia,
    JsonObject? Macronutrientes,
    JsonObject? Micronutrientes,
    IReadOnlyList<string>? Alergenos,
    JsonObject? ModoPreparacion,
    IReadOnlyList<IngredienteDeFichaEntrada>? Ingredientes) : ICommand<FichaNutricionalDto>;
