using System.Text.Json.Nodes;
using Menus.Application.Catalogo.Fichas;
using Menus.Application.Comun;
using Menus.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Menus.Infrastructure.Tests.Mongo;

/// <summary>
/// Pruebas sin MongoDB del almacén de fichas (#283): <see cref="SinAlmacenDeFichas"/> responde 503, y la conversión de
/// <see cref="AlmacenDeFichasEnMongo"/> entre la ficha y el documento no pierde nada. Las pruebas contra un MongoDB real
/// están en <c>Menus.Infrastructure.IntegrationTests</c>.
/// </summary>
public sealed class AlmacenDeFichasTests
{
    private static readonly Guid PlatoId = Guid.Parse("7a1a0000-0000-4000-8000-000000000007");
    private static readonly Guid PolloId = Guid.Parse("a1100000-0000-4000-8000-000000000015");

    private static FichaNutricionalDto FichaDePlato() => new(
        PlatoId,
        "Plato",
        "compuesto",
        new PorcionDeReferenciaDto(1, "ración (350 g)"),
        JsonNode.Parse("""{ "energiaKcal": 486, "proteinasG": 31.5 }""")!.AsObject(),
        JsonNode.Parse("""{ "minerales": { "sodioMg": 620 } }""")!.AsObject(),
        ["apio"],
        JsonNode.Parse("""{ "tecnica": "guiso", "pasos": [ { "orden": 1, "descripcion": "Dorar." } ] }""")!.AsObject(),
        [new IngredienteDeFichaDto(PolloId, "Pollo", JsonNode.Parse("""{ "energiaKcal": 248 }""")!.AsObject())],
        new DateTime(2026, 11, 26, 10, 15, 0, 123, DateTimeKind.Utc));

    [Fact]
    public async Task Sin_MongoDB_leer_la_ficha_responde_servicio_no_disponible()
    {
        var ex = await Assert.ThrowsAsync<ServicioNoDisponibleException>(
            () => new SinAlmacenDeFichas().ObtenerAsync(ClaseDeItem.Alimento, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(SinAlmacenDeFichas.Mensaje, ex.Message);
    }

    [Fact]
    public async Task Sin_MongoDB_guardar_la_ficha_responde_servicio_no_disponible()
        => await Assert.ThrowsAsync<ServicioNoDisponibleException>(
            () => new SinAlmacenDeFichas().GuardarAsync(FichaDePlato(), CancellationToken.None));

    [Fact]
    public void El_documento_lleva_el_origen_con_el_GUID_en_texto_y_los_bloques_como_subdocumentos()
    {
        var documento = AlmacenDeFichasEnMongo.ADocumento(FichaDePlato()).ToBsonDocument();

        Assert.Equal("Plato", documento["origen"]["tabla"].AsString);
        Assert.Equal(PlatoId.ToString("D"), documento["origen"]["id"].AsString);
        Assert.Equal("compuesto", documento["tipo"].AsString);
        Assert.Equal(486, documento["macronutrientes"]["energiaKcal"].AsInt32);
        Assert.Equal(31.5, documento["macronutrientes"]["proteinasG"].AsDouble);
        Assert.Equal(620, documento["micronutrientes"]["minerales"]["sodioMg"].AsInt32);
        Assert.Equal(PolloId.ToString("D"), documento["ingredientes"][0]["alimentoId"].AsString);
        Assert.Equal("Pollo", documento["ingredientes"][0]["nombre"].AsString);
        Assert.Equal(BsonType.DateTime, documento["actualizadoEn"].BsonType);
    }

    [Fact]
    public void Un_bloque_ausente_no_se_guarda_como_null()
    {
        var ficha = new FichaNutricionalDto(
            Guid.NewGuid(), "Alimento", "simple", null, null, null, null, null, null, DateTime.UtcNow);

        var documento = AlmacenDeFichasEnMongo.ADocumento(ficha).ToBsonDocument();

        foreach (var campo in new[] { "porcionReferencia", "macronutrientes", "micronutrientes", "alergenos", "modoPreparacion", "ingredientes" })
        {
            Assert.False(documento.Contains(campo), $"{campo} no debería estar en el documento.");
        }
    }

    [Fact]
    public void Ida_y_vuelta_por_BSON_devuelve_la_misma_ficha()
    {
        var original = FichaDePlato();
        var bson = AlmacenDeFichasEnMongo.ADocumento(original).ToBsonDocument();

        var leida = AlmacenDeFichasEnMongo.ADto(PlatoId, BsonSerializer.Deserialize<FichaNutricionalDocumento>(bson));

        Assert.Equal(original.ItemId, leida.ItemId);
        Assert.Equal(original.Clase, leida.Clase);
        Assert.Equal(original.Tipo, leida.Tipo);
        Assert.Equal(original.PorcionReferencia, leida.PorcionReferencia);
        Assert.True(JsonNode.DeepEquals(original.Macronutrientes, leida.Macronutrientes));
        Assert.True(JsonNode.DeepEquals(original.Micronutrientes, leida.Micronutrientes));
        Assert.True(JsonNode.DeepEquals(original.ModoPreparacion, leida.ModoPreparacion));
        Assert.Equal(original.Alergenos, leida.Alergenos);
        var ingrediente = Assert.Single(leida.Ingredientes!);
        Assert.Equal(PolloId, ingrediente.AlimentoId);
        Assert.Equal("Pollo", ingrediente.Nombre);
        Assert.True(JsonNode.DeepEquals(original.Ingredientes![0].Aporte, ingrediente.Aporte));
        Assert.Equal(original.ActualizadoEn, leida.ActualizadoEn);
        Assert.Equal(DateTimeKind.Utc, leida.ActualizadoEn.Kind);
    }
}
