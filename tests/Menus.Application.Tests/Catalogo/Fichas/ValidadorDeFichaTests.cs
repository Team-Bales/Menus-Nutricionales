using System.Text.Json.Nodes;
using Menus.Application.Catalogo.Fichas;
using Menus.Application.Catalogo.Fichas.GuardarFicha;

namespace Menus.Application.Tests.Catalogo.Fichas;

/// <summary>Pruebas de <see cref="GuardarFichaValidator"/> (#283): RM-04 y nombres de campo guardables (RM-05).</summary>
public sealed class ValidadorDeFichaTests
{
    private readonly GuardarFichaValidator _validador = new();

    private static GuardarFichaCommand Comando(
        PorcionDeReferenciaDto? porcion = null,
        IReadOnlyList<string>? alergenos = null,
        JsonObject? macronutrientes = null,
        JsonObject? modoPreparacion = null,
        IReadOnlyList<IngredienteDeFichaEntrada>? ingredientes = null,
        Guid? itemId = null)
        => new(ClaseDeItem.Plato, itemId ?? Guid.NewGuid(), porcion, macronutrientes, null, alergenos, modoPreparacion, ingredientes);

    private static JsonObject Json(string texto) => JsonNode.Parse(texto)!.AsObject();

    [Fact]
    public void Una_ficha_vacia_es_valida_porque_ningun_bloque_es_obligatorio()
        => Assert.True(_validador.Validate(Comando()).IsValid);

    [Fact]
    public void Una_ficha_completa_y_correcta_es_valida()
    {
        var comando = Comando(
            new PorcionDeReferenciaDto(1, "ración (350 g)"),
            ["apio", "gluten"],
            Json("""{ "energiaKcal": 486, "grasas": { "saturadasG": 3.1 } }"""),
            Json("""{ "pasos": [ { "orden": 1, "descripcion": "Dorar." } ] }"""),
            [new(Guid.NewGuid(), Json("""{ "energiaKcal": 248 }""")), new(Guid.NewGuid(), null)]);

        Assert.True(_validador.Validate(comando).IsValid);
    }

    [Fact]
    public void Sin_item_no_es_valida()
        => Assert.False(_validador.Validate(Comando(itemId: Guid.Empty)).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void La_cantidad_de_la_porcion_tiene_que_ser_un_numero_positivo(double cantidad)
    {
        var resultado = _validador.Validate(Comando(new PorcionDeReferenciaDto(cantidad, "g")));

        Assert.Contains(resultado.Errors, e => e.PropertyName == "porcionReferencia.cantidad");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void La_unidad_de_la_porcion_es_obligatoria(string unidad)
    {
        var resultado = _validador.Validate(Comando(new PorcionDeReferenciaDto(100, unidad)));

        Assert.Contains(resultado.Errors, e => e.PropertyName == "porcionReferencia.unidad");
    }

    [Fact]
    public void Una_lista_de_alergenos_vacia_es_valida_y_significa_sin_alergenos()
        => Assert.True(_validador.Validate(Comando(alergenos: [])).IsValid);

    [Fact]
    public void Un_alergeno_vacio_no_es_valido()
        => Assert.False(_validador.Validate(Comando(alergenos: ["apio", " "])).IsValid);

    [Theory]
    [InlineData("Maní", "mani")]
    [InlineData("Gluten", " gluten ")]
    [InlineData("LECHE", "leche")]
    public void Un_alergeno_repetido_sin_distinguir_mayusculas_ni_acentos_no_es_valido(string uno, string otro)
    {
        var resultado = _validador.Validate(Comando(alergenos: [uno, otro]));

        Assert.Contains(resultado.Errors, e => e.ErrorMessage.Contains("RM-04"));
    }

    [Theory]
    [InlineData("""{ "$set": 1 }""")]
    [InlineData("""{ "energia.kcal": 1 }""")]
    [InlineData("""{ "": 1 }""")]
    [InlineData("""{ "grasas": { "$gt": 1 } }""")]
    public void Un_nombre_de_campo_que_MongoDB_no_puede_guardar_no_es_valido(string macronutrientes)
        => Assert.False(_validador.Validate(Comando(macronutrientes: Json(macronutrientes))).IsValid);

    [Fact]
    public void Un_nombre_invalido_dentro_de_un_arreglo_tambien_se_detecta()
        => Assert.False(_validador.Validate(Comando(modoPreparacion: Json("""{ "pasos": [ { "$where": "x" } ] }"""))).IsValid);

    [Fact]
    public void Un_nombre_invalido_en_el_aporte_de_un_ingrediente_se_detecta()
        => Assert.False(_validador.Validate(Comando(ingredientes: [new(Guid.NewGuid(), Json("""{ "a.b": 1 }"""))])).IsValid);

    [Fact]
    public void Un_alimento_repetido_en_el_desglose_no_es_valido()
    {
        var alimento = Guid.NewGuid();

        Assert.False(_validador.Validate(Comando(ingredientes: [new(alimento, null), new(alimento, null)])).IsValid);
    }

    [Fact]
    public void Un_ingrediente_sin_alimento_no_es_valido()
        => Assert.False(_validador.Validate(Comando(ingredientes: [new(Guid.Empty, null)])).IsValid);
}
