using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Ficha nutricional ampliada de un ítem alimentario, tal como se guarda en la colección
/// <see cref="FichaNutricionalEsquema.NombreColeccion"/> (documento 1 de <c>docs/Ficha nutricional MongoDB</c>, §2).
/// </summary>
/// <remarks>
/// <para>
/// La ficha vive en MongoDB porque sus campos varían de un ítem a otro (RF-04, ADR-005). Se asocia a su ítem de
/// PostgreSQL por <see cref="Origen"/> (documento 2, alternativa B), y el validador de la colección exige lo que toda
/// ficha comparte (RM-01 a RM-05).
/// </para>
/// <para>
/// Los campos opcionales que valen <see langword="null"/> no se guardan: «ausente» significa «no registrado». Una
/// lista vacía sí se guarda; por ejemplo, <c>alergenos = []</c> significa «registrado sin alérgenos».
/// </para>
/// </remarks>
public sealed class FichaNutricionalDocumento
{
    /// <summary>Identificador interno del documento, que genera MongoDB.</summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>El ítem de PostgreSQL al que pertenece la ficha (RM-01).</summary>
    [BsonElement("origen")]
    public OrigenFicha Origen { get; set; } = new();

    /// <summary><c>simple</c> para un alimento, <c>compuesto</c> para un plato (RM-03).</summary>
    [BsonElement("tipo")]
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Porción sobre la que se expresan los valores, por ejemplo 100 g.</summary>
    [BsonElement("porcionReferencia")]
    [BsonIgnoreIfNull]
    public PorcionReferencia? PorcionReferencia { get; set; }

    /// <summary>Energía y macronutrientes; los campos varían de un ítem a otro (RM-05).</summary>
    [BsonElement("macronutrientes")]
    [BsonIgnoreIfNull]
    public BsonDocument? Macronutrientes { get; set; }

    /// <summary>Vitaminas, minerales u otros, en subobjetos variables (RM-05).</summary>
    [BsonElement("micronutrientes")]
    [BsonIgnoreIfNull]
    public BsonDocument? Micronutrientes { get; set; }

    /// <summary>Alérgenos presentes, sin repetir (RM-04). Texto libre; la aplicación normaliza los términos.</summary>
    [BsonElement("alergenos")]
    [BsonIgnoreIfNull]
    public List<string>? Alergenos { get; set; }

    /// <summary>Pasos, tiempos y técnica de preparación (RM-05).</summary>
    [BsonElement("modoPreparacion")]
    [BsonIgnoreIfNull]
    public BsonDocument? ModoPreparacion { get; set; }

    /// <summary>Desglose nutricional por ingrediente; solo en las fichas de plato (RM-03, RM-06).</summary>
    [BsonElement("ingredientes")]
    [BsonIgnoreIfNull]
    public List<IngredienteFicha>? Ingredientes { get; set; }

    /// <summary>Fecha de la última modificación de la ficha, en UTC.</summary>
    [BsonElement("actualizadoEn")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ActualizadoEn { get; set; }
}

/// <summary>Referencia de la ficha a su ítem de PostgreSQL (documento 2, alternativa B).</summary>
public sealed class OrigenFicha
{
    /// <summary>La clase del ítem: <c>Alimento</c> o <c>Plato</c> (RM-02, RD-17).</summary>
    [BsonElement("tabla")]
    public string Tabla { get; set; } = string.Empty;

    /// <summary>
    /// El identificador del ítem, en el formato de <see cref="FichaNutricionalEsquema.Identificador"/>.
    /// </summary>
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;
}

/// <summary>Porción de referencia de los valores nutricionales.</summary>
public sealed class PorcionReferencia
{
    /// <summary>Cantidad de la porción, estrictamente positiva (RM-04).</summary>
    [BsonElement("cantidad")]
    public double Cantidad { get; set; }

    /// <summary>Unidad de la porción, por ejemplo <c>g</c> o <c>ración (350 g)</c>.</summary>
    [BsonElement("unidad")]
    public string Unidad { get; set; } = string.Empty;
}

/// <summary>Aporte de un ingrediente dentro de la ficha de un plato.</summary>
public sealed class IngredienteFicha
{
    /// <summary>
    /// El alimento de la composición del plato, en el formato de <see cref="FichaNutricionalEsquema.Identificador"/>.
    /// </summary>
    [BsonElement("alimentoId")]
    public string AlimentoId { get; set; } = string.Empty;

    /// <summary>Copia de lectura del nombre del alimento; no se valida.</summary>
    [BsonElement("nombre")]
    [BsonIgnoreIfNull]
    public string? Nombre { get; set; }

    /// <summary>El aporte nutricional del ingrediente dentro del plato.</summary>
    [BsonElement("aporte")]
    [BsonIgnoreIfNull]
    public BsonDocument? Aporte { get; set; }
}
