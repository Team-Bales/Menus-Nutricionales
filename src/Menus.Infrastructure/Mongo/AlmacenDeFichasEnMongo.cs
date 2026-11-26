using System.Text.Json.Nodes;
using Menus.Application.Catalogo.Fichas;
using Menus.Application.Comun;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Implementación de <see cref="IAlmacenDeFichas"/> sobre <see cref="MenusMongoContext.Fichas"/> (#283).
/// </summary>
/// <remarks>
/// <para>
/// Guardar es un reemplazo con <c>upsert</c> filtrado por <c>origen</c>: crea la ficha si el ítem no tiene y la
/// reemplaza entera si ya tiene, sin leerla antes. El documento se manda sin <c>_id</c>, así MongoDB conserva el de
/// la ficha que reemplaza o genera uno nuevo. El índice único de RM-08 impide una segunda ficha para el mismo ítem, y
/// MongoDB reintenta solo un <c>upsert</c> que choca con él cuando el filtro es justo la clave del índice.
/// </para>
/// <para>
/// Los bloques libres se convierten entre JSON y BSON con su texto JSON: los números enteros quedan como enteros y
/// los decimales como <c>double</c>, que es lo que acepta el validador.
/// </para>
/// <para>
/// Si MongoDB no responde, se lanza <see cref="ServicioNoDisponibleException"/> (503) en lugar del error del driver.
/// </para>
/// </remarks>
/// <param name="contexto">El contexto de MongoDB.</param>
public sealed class AlmacenDeFichasEnMongo(MenusMongoContext contexto) : IAlmacenDeFichas
{
    /// <summary>Mensaje del 503 cuando MongoDB está configurado pero no responde.</summary>
    public const string MensajeSinRespuesta = "La ficha nutricional no está disponible: MongoDB no responde.";

    private static readonly JsonWriterSettings JsonRelajado = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    /// <inheritdoc />
    public async Task<FichaNutricionalDto?> ObtenerAsync(ClaseDeItem clase, Guid itemId, CancellationToken ct)
    {
        var documento = await Intentar(() => contexto.Fichas.Find(PorOrigen(clase, itemId)).FirstOrDefaultAsync(ct));
        return documento is null ? null : ADto(itemId, documento);
    }

    /// <inheritdoc />
    public async Task GuardarAsync(FichaNutricionalDto ficha, CancellationToken ct)
    {
        var documento = ADocumento(ficha).ToBsonDocument();
        documento.Remove("_id");

        var coleccion = contexto.Fichas.Database.GetCollection<BsonDocument>(FichaNutricionalEsquema.NombreColeccion);
        var filtro = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("origen.tabla", ficha.Clase),
            Builders<BsonDocument>.Filter.Eq("origen.id", FichaNutricionalEsquema.Identificador(ficha.ItemId)));

        await Intentar(() => coleccion.ReplaceOneAsync(filtro, documento, new ReplaceOptions { IsUpsert = true }, ct));
    }

    /// <summary>Convierte la ficha al documento de la colección.</summary>
    /// <param name="ficha">La ficha.</param>
    /// <returns>El documento, sin <c>_id</c> asignado.</returns>
    internal static FichaNutricionalDocumento ADocumento(FichaNutricionalDto ficha) => new()
    {
        Origen = new OrigenFicha { Tabla = ficha.Clase, Id = FichaNutricionalEsquema.Identificador(ficha.ItemId) },
        Tipo = ficha.Tipo,
        PorcionReferencia = ficha.PorcionReferencia is { } p
            ? new PorcionReferencia { Cantidad = p.Cantidad, Unidad = p.Unidad }
            : null,
        Macronutrientes = ABson(ficha.Macronutrientes),
        Micronutrientes = ABson(ficha.Micronutrientes),
        Alergenos = ficha.Alergenos?.ToList(),
        ModoPreparacion = ABson(ficha.ModoPreparacion),
        Ingredientes = ficha.Ingredientes?
            .Select(i => new IngredienteFicha
            {
                AlimentoId = FichaNutricionalEsquema.Identificador(i.AlimentoId),
                Nombre = i.Nombre,
                Aporte = ABson(i.Aporte),
            })
            .ToList(),
        ActualizadoEn = ficha.ActualizadoEn,
    };

    /// <summary>Convierte un documento de la colección a la ficha.</summary>
    /// <param name="itemId">El ítem, el mismo de <c>origen.id</c>.</param>
    /// <param name="documento">El documento.</param>
    /// <returns>La ficha.</returns>
    internal static FichaNutricionalDto ADto(Guid itemId, FichaNutricionalDocumento documento) => new(
        itemId,
        documento.Origen.Tabla,
        documento.Tipo,
        documento.PorcionReferencia is { } p ? new PorcionDeReferenciaDto(p.Cantidad, p.Unidad) : null,
        AJson(documento.Macronutrientes),
        AJson(documento.Micronutrientes),
        documento.Alergenos,
        AJson(documento.ModoPreparacion),
        documento.Ingredientes?
            .Select(i => new IngredienteDeFichaDto(Guid.Parse(i.AlimentoId), i.Nombre, AJson(i.Aporte)))
            .ToList(),
        DateTime.SpecifyKind(documento.ActualizadoEn, DateTimeKind.Utc));

    private static BsonDocument? ABson(JsonObject? bloque)
        => bloque is null ? null : BsonDocument.Parse(bloque.ToJsonString());

    private static JsonObject? AJson(BsonDocument? bloque)
        => bloque is null ? null : JsonNode.Parse(bloque.ToJson(JsonRelajado))!.AsObject();

    private static FilterDefinition<FichaNutricionalDocumento> PorOrigen(ClaseDeItem clase, Guid itemId)
    {
        var filtro = Builders<FichaNutricionalDocumento>.Filter;
        return filtro.And(
            filtro.Eq(f => f.Origen.Tabla, clase == ClaseDeItem.Plato ? "Plato" : "Alimento"),
            filtro.Eq(f => f.Origen.Id, FichaNutricionalEsquema.Identificador(itemId)));
    }

    /// <summary>Traduce a 503 los fallos de conexión con MongoDB; cualquier otro error sigue su curso.</summary>
    private static async Task<T> Intentar<T>(Func<Task<T>> operacion)
    {
        try
        {
            return await operacion();
        }
        catch (Exception ex) when (ex is MongoConnectionException or TimeoutException)
        {
            throw new ServicioNoDisponibleException(MensajeSinRespuesta, ex);
        }
    }
}
