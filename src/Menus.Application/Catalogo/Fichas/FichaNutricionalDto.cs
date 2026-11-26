using System.Text.Json.Nodes;

namespace Menus.Application.Catalogo.Fichas;

/// <summary>Clase del ítem al que pertenece una ficha: la ruta dice cuál es (#283).</summary>
public enum ClaseDeItem
{
    /// <summary>Alimento simple: su ficha es <c>simple</c> y no lleva desglose de ingredientes.</summary>
    Alimento,

    /// <summary>Plato: su ficha es <c>compuesto</c> y puede llevar el desglose de sus ingredientes.</summary>
    Plato,
}

/// <summary>
/// Ficha nutricional ampliada de un alimento o un plato (RF-04), con la forma del documento 1 de
/// <c>docs/Ficha nutricional MongoDB</c> y los nombres de sus campos.
/// </summary>
/// <remarks>
/// Todo bloque es opcional, y un bloque ausente es <see langword="null"/>: «no aplica o no se conoce» (documento 1,
/// §1). Los bloques nutricionales no tienen campos fijos (RM-05), así que viajan como objetos JSON.
/// </remarks>
/// <param name="ItemId">El alimento o el plato.</param>
/// <param name="Clase"><c>Alimento</c> o <c>Plato</c> (<c>origen.tabla</c>, RM-02).</param>
/// <param name="Tipo"><c>simple</c> para un alimento y <c>compuesto</c> para un plato (RM-03).</param>
/// <param name="PorcionReferencia">Porción sobre la que se expresan los valores.</param>
/// <param name="Macronutrientes">Energía y macronutrientes.</param>
/// <param name="Micronutrientes">Vitaminas, minerales u otros.</param>
/// <param name="Alergenos">Alérgenos presentes; <see langword="null"/> es «no registrado» y vacío, «sin alérgenos».</param>
/// <param name="ModoPreparacion">Pasos, tiempos y técnica.</param>
/// <param name="Ingredientes">Desglose por ingrediente; solo en la ficha de un plato (RM-03, RM-06).</param>
/// <param name="ActualizadoEn">Fecha de la última modificación, en UTC.</param>
public sealed record FichaNutricionalDto(
    Guid ItemId,
    string Clase,
    string Tipo,
    PorcionDeReferenciaDto? PorcionReferencia,
    JsonObject? Macronutrientes,
    JsonObject? Micronutrientes,
    IReadOnlyList<string>? Alergenos,
    JsonObject? ModoPreparacion,
    IReadOnlyList<IngredienteDeFichaDto>? Ingredientes,
    DateTime ActualizadoEn);

/// <summary>Porción sobre la que se expresan los valores de la ficha (por ejemplo, 100 g).</summary>
/// <param name="Cantidad">Cantidad, positiva (RM-04).</param>
/// <param name="Unidad">Unidad, por ejemplo «g» o «ración (350 g)».</param>
public sealed record PorcionDeReferenciaDto(double Cantidad, string Unidad);

/// <summary>Aporte de un ingrediente dentro de la ficha de un plato.</summary>
/// <param name="AlimentoId">El alimento, que está en la composición del plato (RM-06).</param>
/// <param name="Nombre">Copia del nombre del alimento, para leer la ficha sin consultar PostgreSQL.</param>
/// <param name="Aporte">Lo que aporta al plato, con los mismos nombres que los macronutrientes.</param>
public sealed record IngredienteDeFichaDto(Guid AlimentoId, string? Nombre, JsonObject? Aporte);

/// <summary>Un ingrediente del desglose tal como lo manda quien escribe la ficha: el nombre lo pone el servidor.</summary>
/// <param name="AlimentoId">El alimento, que tiene que estar en la composición del plato (RM-06).</param>
/// <param name="Aporte">Lo que aporta al plato.</param>
public sealed record IngredienteDeFichaEntrada(Guid AlimentoId, JsonObject? Aporte);
