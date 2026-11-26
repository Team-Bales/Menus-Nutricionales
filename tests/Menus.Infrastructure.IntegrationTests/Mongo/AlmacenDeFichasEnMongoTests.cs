using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Menus.Application.Catalogo.Fichas;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Mongo;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Mongo;

/// <summary>
/// Pruebas de <see cref="AlmacenDeFichasEnMongo"/> contra MongoDB real (#283): guardar crea o reemplaza la única ficha
/// del ítem (RM-08), lo guardado cumple el validador de la colección (RM-01 a RM-05) y se lee igual que se escribió.
/// </summary>
/// <remarks>
/// Cada prueba usa una base nueva (<see cref="MongoFixture.NuevoContextoAsync"/>), con el validador y el índice único.
/// </remarks>
[Collection("MongoDB")]
public sealed class AlmacenDeFichasEnMongoTests : IAsyncLifetime
{
    private readonly MongoFixture _mongo;
    private MenusMongoContext _contexto = null!;
    private IMongoDatabase _baseDeDatos = null!;

    public AlmacenDeFichasEnMongoTests(MongoFixture mongo)
    {
        _mongo = mongo;
    }

    public async Task InitializeAsync() => (_contexto, _baseDeDatos) = await _mongo.NuevoContextoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private AlmacenDeFichasEnMongo Almacen() => new(_contexto);

    private static FichaNutricionalDto FichaDePlato(Guid platoId, Guid alimentoId, int energia) => new(
        platoId,
        "Plato",
        "compuesto",
        new PorcionDeReferenciaDto(1, "ración (350 g)"),
        new JsonObject { ["energiaKcal"] = energia, ["proteinasG"] = 31.5 },
        JsonNode.Parse("""{ "minerales": { "sodioMg": 620 } }""")!.AsObject(),
        ["apio"],
        JsonNode.Parse("""{ "tecnica": "guiso", "pasos": [ { "orden": 1, "descripcion": "Dorar." } ] }""")!.AsObject(),
        [new IngredienteDeFichaDto(alimentoId, "Pollo", new JsonObject { ["energiaKcal"] = 248 })],
        new DateTime(2026, 11, 26, 10, 15, 0, 123, DateTimeKind.Utc));

    private static FichaNutricionalDto FichaDeAlimento(Guid alimentoId) => new(
        alimentoId, "Alimento", "simple", null, new JsonObject { ["energiaKcal"] = 52 }, null, [], null, null, DateTime.UtcNow);

    [Fact]
    public async Task Guardar_crea_la_ficha_y_Obtener_la_devuelve_igual()
    {
        var plato = Guid.NewGuid();
        var pollo = Guid.NewGuid();
        var original = FichaDePlato(plato, pollo, 486);

        await Almacen().GuardarAsync(original, CancellationToken.None);
        var leida = await Almacen().ObtenerAsync(ClaseDeItem.Plato, plato, CancellationToken.None);

        Assert.NotNull(leida);
        Assert.Equal("Plato", leida!.Clase);
        Assert.Equal("compuesto", leida.Tipo);
        Assert.Equal(original.PorcionReferencia, leida.PorcionReferencia);
        Assert.True(JsonNode.DeepEquals(original.Macronutrientes, leida.Macronutrientes));
        Assert.True(JsonNode.DeepEquals(original.Micronutrientes, leida.Micronutrientes));
        Assert.True(JsonNode.DeepEquals(original.ModoPreparacion, leida.ModoPreparacion));
        Assert.Equal(original.Alergenos, leida.Alergenos);
        var ingrediente = Assert.Single(leida.Ingredientes!);
        Assert.Equal(pollo, ingrediente.AlimentoId);
        Assert.Equal("Pollo", ingrediente.Nombre);
        Assert.Equal(original.ActualizadoEn, leida.ActualizadoEn);
    }

    [Fact]
    public async Task Guardar_dos_veces_reemplaza_la_ficha_sin_crear_otra_y_conserva_el_id()
    {
        var plato = Guid.NewGuid();
        var pollo = Guid.NewGuid();
        await Almacen().GuardarAsync(FichaDePlato(plato, pollo, 486), CancellationToken.None);
        var antes = await LeerCrudaAsync("Plato", plato);

        await Almacen().GuardarAsync(FichaDePlato(plato, pollo, 500) with { Alergenos = null }, CancellationToken.None);

        Assert.Equal(1, await _contexto.Fichas.CountDocumentsAsync(FilterDefinition<FichaNutricionalDocumento>.Empty));
        var despues = await LeerCrudaAsync("Plato", plato);
        Assert.Equal(antes["_id"], despues["_id"]);
        Assert.Equal(500, despues["macronutrientes"]["energiaKcal"].AsInt32);
        Assert.False(despues.Contains("alergenos"), "Reemplazar quita lo que la ficha nueva no trae.");
    }

    [Fact]
    public async Task Las_fichas_de_un_alimento_y_de_un_plato_con_el_mismo_id_no_se_pisan()
    {
        var id = Guid.NewGuid();

        await Almacen().GuardarAsync(FichaDeAlimento(id), CancellationToken.None);
        await Almacen().GuardarAsync(FichaDePlato(id, Guid.NewGuid(), 486), CancellationToken.None);

        Assert.Equal("simple", (await Almacen().ObtenerAsync(ClaseDeItem.Alimento, id, CancellationToken.None))!.Tipo);
        Assert.Equal("compuesto", (await Almacen().ObtenerAsync(ClaseDeItem.Plato, id, CancellationToken.None))!.Tipo);
    }

    [Fact]
    public async Task Obtener_sin_ficha_devuelve_null()
        => Assert.Null(await Almacen().ObtenerAsync(ClaseDeItem.Alimento, Guid.NewGuid(), CancellationToken.None));

    [Fact]
    public async Task Una_ficha_de_alimento_con_lista_de_alergenos_vacia_se_guarda_como_arreglo_vacio()
    {
        var alimento = Guid.NewGuid();

        await Almacen().GuardarAsync(FichaDeAlimento(alimento), CancellationToken.None);

        var cruda = await LeerCrudaAsync("Alimento", alimento);
        Assert.Equal(new BsonArray(), cruda["alergenos"]);
        Assert.False(cruda.Contains("ingredientes"));
    }

    [Fact]
    public async Task La_inicializacion_al_arrancar_deja_la_coleccion_con_su_indice_unico()
    {
        var nombre = $"menus_{Guid.NewGuid():N}";
        var contexto = new MenusMongoContext(_mongo.Cliente, nombre);
        var servicio = new InicializacionDeFichasHostedService(
            contexto, NullLogger<InicializacionDeFichasHostedService>.Instance);

        await servicio.StartAsync(CancellationToken.None);
        await servicio.ExecuteTask!;

        using var indices = await contexto.Fichas.Indexes.ListAsync();
        var nombres = (await indices.ToListAsync()).ConvertAll(i => i["name"].AsString);
        Assert.Contains(FichaNutricionalEsquema.NombreIndiceOrigen, nombres);
    }

    private async Task<BsonDocument> LeerCrudaAsync(string tabla, Guid id)
        => await _baseDeDatos.GetCollection<BsonDocument>(FichaNutricionalEsquema.NombreColeccion)
            .Find(Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("origen.tabla", tabla),
                Builders<BsonDocument>.Filter.Eq("origen.id", FichaNutricionalEsquema.Identificador(id))))
            .FirstAsync();
}
