using MongoDB.Bson;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// La colección de fichas nutricionales: su nombre, su índice único y su validador <c>$jsonSchema</c>.
/// </summary>
/// <remarks>
/// <para>
/// El validador es el del documento 1 de <c>docs/Ficha nutricional MongoDB</c> (§5), con una diferencia: los
/// identificadores de este proyecto son <see cref="Guid"/>, no enteros. <c>origen.id</c> e
/// <c>ingredientes[].alimentoId</c> se guardan como cadena con el GUID en formato canónico y en minúsculas
/// (<see cref="Identificador"/>), y el validador lo exige con un patrón. Así no se depende de la representación de
/// GUID del driver.
/// </para>
/// <para>Correspondencia con las reglas del informe (§4.3):</para>
/// <list type="bullet">
/// <item>RM-01: <c>origen</c>, <c>tipo</c> y <c>actualizadoEn</c> son obligatorios, y <c>origen</c> lleva <c>tabla</c> e <c>id</c>.</item>
/// <item>RM-02: <c>origen.tabla</c> es <c>Alimento</c> o <c>Plato</c>.</item>
/// <item>RM-03: <c>tipo</c> es <c>simple</c> o <c>compuesto</c>; el <c>anyOf</c> exige alimento con ficha simple y sin
/// <c>ingredientes</c>, o plato con ficha compuesta (con o sin <c>ingredientes</c>).</item>
/// <item>RM-04: la cantidad de la porción es positiva y los alérgenos no se repiten. MongoDB usa el borrador 4 de JSON
/// Schema, donde <c>exclusiveMinimum</c> es booleano y acompaña a <c>minimum</c>.</item>
/// <item>RM-05: <c>macronutrientes</c>, <c>micronutrientes</c> y <c>modoPreparacion</c> son objetos sin campos
/// obligatorios.</item>
/// <item>RM-08: una ficha por ítem, con el índice único <see cref="NombreIndiceOrigen"/> sobre
/// (<c>origen.tabla</c>, <c>origen.id</c>). No es parte del validador; lo crea
/// <see cref="MenusMongoContext.InicializarAsync"/>.</item>
/// </list>
/// </remarks>
public static class FichaNutricionalEsquema
{
    /// <summary>El nombre de la colección: una sola para alimentos y platos.</summary>
    public const string NombreColeccion = "fichas_nutricionales";

    /// <summary>El nombre del índice único de RM-08.</summary>
    public const string NombreIndiceOrigen = "ux_fichas_origen";

    /// <summary>Patrón de un GUID canónico en minúsculas, el formato de <see cref="Identificador"/>.</summary>
    public const string PatronIdentificador = "^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";

    /// <summary>Convierte el identificador de un ítem al formato que se guarda en la ficha.</summary>
    /// <param name="id">El identificador del ítem en PostgreSQL.</param>
    /// <returns>El GUID en formato canónico (<c>D</c>) y en minúsculas.</returns>
    public static string Identificador(Guid id) => id.ToString("D");

    /// <summary>Devuelve el validador <c>$jsonSchema</c> de la colección (RM-01 a RM-05).</summary>
    /// <returns>Un documento nuevo con el validador, listo para <c>create</c> o <c>collMod</c>.</returns>
    public static BsonDocument Validador() => BsonDocument.Parse($$"""
        {
          "$jsonSchema": {
            "bsonType": "object",
            "required": ["origen", "tipo", "actualizadoEn"],
            "properties": {
              "origen": {
                "bsonType": "object",
                "required": ["tabla", "id"],
                "properties": {
                  "tabla": { "enum": ["Alimento", "Plato"] },
                  "id": { "bsonType": "string", "pattern": "{{PatronIdentificador}}" }
                }
              },
              "tipo": { "enum": ["simple", "compuesto"] },
              "porcionReferencia": {
                "bsonType": "object",
                "required": ["cantidad", "unidad"],
                "properties": {
                  "cantidad": { "bsonType": ["int", "double"], "minimum": 0, "exclusiveMinimum": true }
                }
              },
              "macronutrientes": { "bsonType": "object" },
              "micronutrientes": { "bsonType": "object" },
              "alergenos": { "bsonType": "array", "items": { "bsonType": "string" }, "uniqueItems": true },
              "modoPreparacion": { "bsonType": "object" },
              "ingredientes": {
                "bsonType": "array",
                "items": {
                  "bsonType": "object",
                  "required": ["alimentoId"],
                  "properties": {
                    "alimentoId": { "bsonType": "string", "pattern": "{{PatronIdentificador}}" }
                  }
                }
              },
              "actualizadoEn": { "bsonType": "date" }
            },
            "anyOf": [
              {
                "properties": {
                  "origen": { "properties": { "tabla": { "enum": ["Alimento"] } } },
                  "tipo": { "enum": ["simple"] }
                },
                "not": { "required": ["ingredientes"] }
              },
              {
                "properties": {
                  "origen": { "properties": { "tabla": { "enum": ["Plato"] } } },
                  "tipo": { "enum": ["compuesto"] }
                }
              }
            ]
          }
        }
        """);
}
