using System.Text.Json.Nodes;
using Menus.Application.Catalogo.Fichas;
using Menus.Application.Catalogo.Fichas.GuardarFicha;

namespace Menus.Api.Controladores;

/// <summary>
/// Cuerpo de <c>PUT /api/alimentos/{id}/ficha</c> y <c>PUT /api/platos/{id}/ficha</c> (#283): la ficha completa, que
/// reemplaza a la anterior. <c>origen</c>, <c>tipo</c> y <c>actualizadoEn</c> los pone el servidor.
/// </summary>
/// <param name="PorcionReferencia">Porción sobre la que se expresan los valores.</param>
/// <param name="Macronutrientes">Energía y macronutrientes, con campos libres (RM-05).</param>
/// <param name="Micronutrientes">Vitaminas, minerales u otros, con campos libres (RM-05).</param>
/// <param name="Alergenos">Alérgenos presentes; <c>[]</c> es «sin alérgenos» y ausente, «no registrado».</param>
/// <param name="ModoPreparacion">Pasos, tiempos y técnica, con campos libres (RM-05).</param>
/// <param name="Ingredientes">Desglose por ingrediente: solo en la ficha de un plato, y con sus mismos alimentos (RM-06).</param>
public sealed record CuerpoDeFicha(
    PorcionDeReferenciaDto? PorcionReferencia,
    JsonObject? Macronutrientes,
    JsonObject? Micronutrientes,
    IReadOnlyList<string>? Alergenos,
    JsonObject? ModoPreparacion,
    IReadOnlyList<IngredienteDeFichaEntrada>? Ingredientes)
{
    /// <summary>Arma el comando con la clase y el ítem de la ruta.</summary>
    /// <param name="clase">Clase del ítem, según el controlador.</param>
    /// <param name="itemId">El ítem de la ruta.</param>
    /// <returns>El comando.</returns>
    public GuardarFichaCommand AComando(ClaseDeItem clase, Guid itemId) => new(
        clase,
        itemId,
        PorcionReferencia,
        Macronutrientes,
        Micronutrientes,
        Alergenos,
        ModoPreparacion,
        Ingredientes);
}
