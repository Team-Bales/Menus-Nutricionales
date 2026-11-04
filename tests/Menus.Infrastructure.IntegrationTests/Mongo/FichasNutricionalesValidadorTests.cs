using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Mongo;

/// <summary>
/// Pruebas del validador <c>$jsonSchema</c> (RM-01 a RM-05) y del índice único (RM-08) de la colección de fichas,
/// contra un MongoDB real.
/// </summary>
/// <remarks>
/// Un rechazo del validador es un <see cref="MongoWriteException"/> con el código 121 (<c>DocumentValidationFailure</c>);
/// uno del índice, la categoría <see cref="ServerErrorCategory.DuplicateKey"/> (código 11000). Las pruebas
/// distinguen los dos.
/// </remarks>
[Collection("MongoDB")]
public sealed class FichasNutricionalesValidadorTests(MongoFixture fixture)
{
    private const int CodigoValidacion = 121;

    private static readonly DateTime Fecha = new(2026, 9, 30, 10, 15, 0, DateTimeKind.Utc);

    private static string NuevoId() => FichaNutricionalEsquema.Identificador(Guid.NewGuid());

    /// <summary>Una ficha simple válida, como el ejemplo del documento 1, §3.</summary>
    private static BsonDocument FichaSimple(string? id = null) => new()
    {
        { "origen", new BsonDocument { { "tabla", "Alimento" }, { "id", id ?? NuevoId() } } },
        { "tipo", "simple" },
        { "porcionReferencia", new BsonDocument { { "cantidad", 100 }, { "unidad", "g" } } },
        { "macronutrientes", new BsonDocument { { "energiaKcal", 52 }, { "carbohidratosG", 13.8 } } },
        { "alergenos", new BsonArray() },
        { "actualizadoEn", Fecha },
    };

    /// <summary>Una ficha compuesta válida con desglose por ingrediente, como el ejemplo del documento 1, §4.</summary>
    private static BsonDocument FichaCompuesta() => new()
    {
        { "origen", new BsonDocument { { "tabla", "Plato" }, { "id", NuevoId() } } },
        { "tipo", "compuesto" },
        { "porcionReferencia", new BsonDocument { { "cantidad", 1 }, { "unidad", "ración (350 g)" } } },
        { "alergenos", new BsonArray { "apio" } },
        { "modoPreparacion", new BsonDocument { { "tecnica", "guiso" }, { "tiempoTotalMin", 55 } } },
        {
            "ingredientes", new BsonArray
            {
                new BsonDocument { { "alimentoId", NuevoId() }, { "nombre", "Pollo" }, { "aporte", new BsonDocument("energiaKcal", 248) } },
                new BsonDocument { { "alimentoId", NuevoId() }, { "nombre", "Arroz" } },
            }
        },
        { "actualizadoEn", Fecha },
    };

    private static async Task<IMongoCollection<BsonDocument>> ColeccionAsync(MongoFixture f)
    {
        var (_, baseDeDatos) = await f.NuevoContextoAsync();
        return baseDeDatos.GetCollection<BsonDocument>(FichaNutricionalEsquema.NombreColeccion);
    }

    private static async Task RechazaPorValidadorAsync(IMongoCollection<BsonDocument> coleccion, BsonDocument ficha)
    {
        var ex = await Assert.ThrowsAsync<MongoWriteException>(() => coleccion.InsertOneAsync(ficha));
        Assert.Equal(CodigoValidacion, ex.WriteError.Code);
    }

    [Fact]
    public async Task Acepta_una_ficha_simple_valida()
    {
        var coleccion = await ColeccionAsync(fixture);

        await coleccion.InsertOneAsync(FichaSimple());

        Assert.Equal(1, await coleccion.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    [Fact]
    public async Task Acepta_una_ficha_compuesta_con_y_sin_ingredientes()
    {
        var coleccion = await ColeccionAsync(fixture);
        var sinIngredientes = FichaCompuesta();
        sinIngredientes.Remove("ingredientes");

        await coleccion.InsertOneAsync(FichaCompuesta());
        await coleccion.InsertOneAsync(sinIngredientes);

        Assert.Equal(2, await coleccion.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    [Theory]
    [InlineData("origen")]
    [InlineData("origen.id")]
    [InlineData("origen.tabla")]
    [InlineData("tipo")]
    [InlineData("actualizadoEn")]
    public async Task RM01_rechaza_una_ficha_sin_un_campo_obligatorio(string campo)
    {
        var coleccion = await ColeccionAsync(fixture);
        var ficha = FichaSimple();
        if (campo.StartsWith("origen.", StringComparison.Ordinal))
        {
            ficha["origen"].AsBsonDocument.Remove(campo["origen.".Length..]);
        }
        else
        {
            ficha.Remove(campo);
        }

        await RechazaPorValidadorAsync(coleccion, ficha);
    }

    [Fact]
    public async Task RM01_rechaza_un_id_que_no_es_un_guid_canonico()
    {
        var coleccion = await ColeccionAsync(fixture);
        var conEntero = FichaSimple();
        conEntero["origen"]["id"] = 42;
        var conMayusculas = FichaSimple(Guid.NewGuid().ToString("D").ToUpperInvariant());

        await RechazaPorValidadorAsync(coleccion, conEntero);
        await RechazaPorValidadorAsync(coleccion, conMayusculas);
    }

    [Fact]
    public async Task RM02_rechaza_una_tabla_que_no_es_Alimento_ni_Plato()
    {
        var coleccion = await ColeccionAsync(fixture);
        var ficha = FichaSimple();
        ficha["origen"]["tabla"] = "Receta";

        await RechazaPorValidadorAsync(coleccion, ficha);
    }

    [Fact]
    public async Task RM03_rechaza_la_clase_incoherente_con_el_tipo_y_los_ingredientes_en_un_alimento()
    {
        var coleccion = await ColeccionAsync(fixture);
        var alimentoCompuesto = FichaSimple();
        alimentoCompuesto["tipo"] = "compuesto";
        var platoSimple = FichaCompuesta();
        platoSimple["tipo"] = "simple";
        var alimentoConIngredientes = FichaSimple();
        alimentoConIngredientes["ingredientes"] = new BsonArray { new BsonDocument("alimentoId", NuevoId()) };

        await RechazaPorValidadorAsync(coleccion, alimentoCompuesto);
        await RechazaPorValidadorAsync(coleccion, platoSimple);
        await RechazaPorValidadorAsync(coleccion, alimentoConIngredientes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RM04_rechaza_una_porcion_que_no_es_positiva(int cantidad)
    {
        var coleccion = await ColeccionAsync(fixture);
        var ficha = FichaSimple();
        ficha["porcionReferencia"]["cantidad"] = cantidad;

        await RechazaPorValidadorAsync(coleccion, ficha);
    }

    [Fact]
    public async Task RM04_rechaza_alergenos_repetidos()
    {
        var coleccion = await ColeccionAsync(fixture);
        var ficha = FichaSimple();
        ficha["alergenos"] = new BsonArray { "gluten", "gluten" };

        await RechazaPorValidadorAsync(coleccion, ficha);
    }

    [Fact]
    public async Task RM04_acepta_sin_alergenos_y_con_alergenos_distintos()
    {
        var coleccion = await ColeccionAsync(fixture);
        var conAlergenos = FichaSimple();
        conAlergenos["alergenos"] = new BsonArray { "gluten", "lactosa" };

        await coleccion.InsertOneAsync(FichaSimple());
        await coleccion.InsertOneAsync(conAlergenos);

        Assert.Equal(2, await coleccion.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    [Fact]
    public async Task RM05_acepta_solo_los_obligatorios_y_los_bloques_vacios()
    {
        var coleccion = await ColeccionAsync(fixture);
        var minima = new BsonDocument
        {
            { "origen", new BsonDocument { { "tabla", "Alimento" }, { "id", NuevoId() } } },
            { "tipo", "simple" },
            { "actualizadoEn", Fecha },
        };
        var conBloquesVacios = new BsonDocument
        {
            { "origen", new BsonDocument { { "tabla", "Plato" }, { "id", NuevoId() } } },
            { "tipo", "compuesto" },
            { "macronutrientes", new BsonDocument() },
            { "micronutrientes", new BsonDocument() },
            { "modoPreparacion", new BsonDocument() },
            { "actualizadoEn", Fecha },
        };

        await coleccion.InsertOneAsync(minima);
        await coleccion.InsertOneAsync(conBloquesVacios);

        Assert.Equal(2, await coleccion.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    [Fact]
    public async Task RM08_rechaza_una_segunda_ficha_del_mismo_item_y_acepta_el_mismo_id_en_otra_tabla()
    {
        var coleccion = await ColeccionAsync(fixture);
        var id = NuevoId();
        var plato = FichaCompuesta();
        plato["origen"]["id"] = id;

        await coleccion.InsertOneAsync(FichaSimple(id));
        var ex = await Assert.ThrowsAsync<MongoWriteException>(() => coleccion.InsertOneAsync(FichaSimple(id)));
        await coleccion.InsertOneAsync(plato);

        Assert.Equal(ServerErrorCategory.DuplicateKey, ex.WriteError.Category);
        Assert.NotEqual(CodigoValidacion, ex.WriteError.Code);
        Assert.Equal(2, await coleccion.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    [Fact]
    public async Task InicializarAsync_es_idempotente_y_restaura_el_validador()
    {
        var (contexto, baseDeDatos) = await fixture.NuevoContextoAsync();

        await contexto.InicializarAsync();
        await baseDeDatos.RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "collMod", FichaNutricionalEsquema.NombreColeccion },
            { "validator", new BsonDocument() },
        });
        Assert.Empty(await ValidadorActualAsync(baseDeDatos));
        await contexto.InicializarAsync();

        Assert.Equal(FichaNutricionalEsquema.Validador(), await ValidadorActualAsync(baseDeDatos));
        var indices = await (await contexto.Fichas.Indexes.ListAsync()).ToListAsync();
        var indice = Assert.Single(indices, i => i["name"] == FichaNutricionalEsquema.NombreIndiceOrigen);
        Assert.True(indice["unique"].AsBoolean);
    }

    [Fact]
    public async Task El_documento_tipado_se_guarda_sin_los_campos_opcionales_nulos()
    {
        var (contexto, baseDeDatos) = await fixture.NuevoContextoAsync();
        var ficha = new FichaNutricionalDocumento
        {
            Origen = new OrigenFicha { Tabla = "Alimento", Id = NuevoId() },
            Tipo = "simple",
            Alergenos = [],
            ActualizadoEn = Fecha,
        };

        await contexto.Fichas.InsertOneAsync(ficha);

        var guardado = await baseDeDatos.GetCollection<BsonDocument>(FichaNutricionalEsquema.NombreColeccion)
            .Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        Assert.Equal(new List<string> { "_id", "origen", "tipo", "alergenos", "actualizadoEn" }, guardado.Names.ToList());
        Assert.Empty(guardado["alergenos"].AsBsonArray);
        Assert.Equal(Fecha, guardado["actualizadoEn"].ToUniversalTime());
    }

    private static async Task<BsonDocument> ValidadorActualAsync(IMongoDatabase baseDeDatos)
    {
        var colecciones = await (await baseDeDatos.ListCollectionsAsync(new ListCollectionsOptions
        {
            Filter = new BsonDocument("name", FichaNutricionalEsquema.NombreColeccion),
        })).ToListAsync();
        return colecciones.Single()["options"].AsBsonDocument.GetValue("validator", new BsonDocument()).AsBsonDocument;
    }
}
