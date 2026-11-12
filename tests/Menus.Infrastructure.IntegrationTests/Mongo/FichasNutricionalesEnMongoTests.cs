using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Mongo;

/// <summary>
/// Pruebas de <see cref="FichasNutricionalesEnMongo"/> contra MongoDB real: lo que los casos de uso del catálogo
/// mantienen de la ficha después de confirmar en PostgreSQL (RM-09 y R8-06).
/// </summary>
/// <remarks>
/// Solo necesita MongoDB: el puerto recibe ítems ya confirmados, así que no hace falta sembrar PostgreSQL. Cada prueba
/// usa una base nueva (<see cref="MongoFixture.NuevoContextoAsync"/>), con el validador y el índice de la colección.
/// </remarks>
[Collection("MongoDB")]
public sealed class FichasNutricionalesEnMongoTests : IAsyncLifetime
{
    private static readonly Guid Descriptor = Guid.NewGuid();
    private static readonly Guid Grupo = Guid.Parse("11111111-0000-0000-0000-000000000001");

    private readonly MongoFixture _mongo;
    private MenusMongoContext _contexto = null!;

    public FichasNutricionalesEnMongoTests(MongoFixture mongo)
    {
        _mongo = mongo;
    }

    public async Task InitializeAsync() => (_contexto, _) = await _mongo.NuevoContextoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private FichasNutricionalesEnMongo Fichas()
    {
        var almacen = new FichaNutricionalStore(_contexto);
        return new FichasNutricionalesEnMongo(almacen, almacen, NullLogger<FichasNutricionalesEnMongo>.Instance);
    }

    private static Alimento NuevoAlimento(string nombre) => new(
        Guid.NewGuid(), nombre, Grupo, TipoPreparacion.PlatoFuerte.Id, NivelCalorico.Medio.Id, Descriptor);

    private static Plato NuevoPlato() => new(
        Guid.NewGuid(), "Ensalada", Grupo, TipoPreparacion.PlatoFuerte.Id, NivelCalorico.Medio.Id, Descriptor);

    private static FichaNutricionalDocumento Ficha(string tabla, Guid id, params Guid[] ingredientes) => new()
    {
        Origen = new OrigenFicha { Tabla = tabla, Id = FichaNutricionalEsquema.Identificador(id) },
        Tipo = tabla == "Plato" ? "compuesto" : "simple",
        Ingredientes = tabla == "Plato"
            ? [.. ingredientes.Select(i => new IngredienteFicha { AlimentoId = FichaNutricionalEsquema.Identificador(i) })]
            : null,
        ActualizadoEn = DateTime.UtcNow.AddDays(-1),
    };

    private async Task<FichaNutricionalDocumento?> LeerAsync(string tabla, Guid id)
        => await _contexto.Fichas
            .Find(f => f.Origen.Tabla == tabla && f.Origen.Id == FichaNutricionalEsquema.Identificador(id))
            .FirstOrDefaultAsync();

    // ---- RM-09 ----

    [Fact]
    public async Task EliminarAsync_borra_la_ficha_del_alimento_y_no_la_de_otro_item()
    {
        var alimento = NuevoAlimento("Avena");
        var otro = NuevoAlimento("Arroz");
        await _contexto.Fichas.InsertManyAsync([Ficha("Alimento", alimento.Id), Ficha("Alimento", otro.Id)]);

        await Fichas().EliminarAsync(alimento);

        Assert.Null(await LeerAsync("Alimento", alimento.Id));
        Assert.NotNull(await LeerAsync("Alimento", otro.Id));
    }

    [Fact]
    public async Task EliminarAsync_de_un_plato_busca_su_ficha_en_la_tabla_Plato()
    {
        var plato = NuevoPlato();
        await _contexto.Fichas.InsertOneAsync(Ficha("Plato", plato.Id));

        await Fichas().EliminarAsync(plato);

        Assert.Null(await LeerAsync("Plato", plato.Id));
    }

    [Fact]
    public async Task EliminarAsync_sin_ficha_no_falla()
        => await Fichas().EliminarAsync(NuevoAlimento("Sin ficha"));

    // ---- R8-06 ----

    [Fact]
    public async Task AgregarIngredienteAsync_suma_el_alimento_con_su_nombre_y_actualiza_la_fecha()
    {
        var plato = NuevoPlato();
        var lechuga = NuevoAlimento("Lechuga");
        var tomate = NuevoAlimento("Tomate");
        await _contexto.Fichas.InsertOneAsync(Ficha("Plato", plato.Id, lechuga.Id));

        await Fichas().AgregarIngredienteAsync(plato.Id, tomate);

        var ficha = await LeerAsync("Plato", plato.Id);
        Assert.Equal(
            [FichaNutricionalEsquema.Identificador(lechuga.Id), FichaNutricionalEsquema.Identificador(tomate.Id)],
            ficha!.Ingredientes!.Select(i => i.AlimentoId));
        Assert.Equal("Tomate", ficha.Ingredientes![1].Nombre);
        Assert.True(ficha.ActualizadoEn > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task AgregarIngredienteAsync_dos_veces_no_repite_el_alimento()
    {
        var plato = NuevoPlato();
        var tomate = NuevoAlimento("Tomate");
        await _contexto.Fichas.InsertOneAsync(Ficha("Plato", plato.Id, Guid.NewGuid()));

        await Fichas().AgregarIngredienteAsync(plato.Id, tomate);
        await Fichas().AgregarIngredienteAsync(plato.Id, tomate);

        var ficha = await LeerAsync("Plato", plato.Id);
        Assert.Single(ficha!.Ingredientes!, i => i.AlimentoId == FichaNutricionalEsquema.Identificador(tomate.Id));
    }

    [Fact]
    public async Task AgregarIngredienteAsync_no_crea_un_desglose_parcial_en_una_ficha_que_no_lo_tiene()
    {
        var plato = NuevoPlato();
        var sinDesglose = Ficha("Plato", plato.Id);
        sinDesglose.Ingredientes = null;
        await _contexto.Fichas.InsertOneAsync(sinDesglose);

        await Fichas().AgregarIngredienteAsync(plato.Id, NuevoAlimento("Tomate"));

        Assert.Null((await LeerAsync("Plato", plato.Id))!.Ingredientes);
    }

    [Fact]
    public async Task QuitarIngredienteAsync_saca_el_alimento_del_desglose()
    {
        var plato = NuevoPlato();
        var lechuga = Guid.NewGuid();
        var tomate = Guid.NewGuid();
        await _contexto.Fichas.InsertOneAsync(Ficha("Plato", plato.Id, lechuga, tomate));

        await Fichas().QuitarIngredienteAsync(plato.Id, tomate);

        var ficha = await LeerAsync("Plato", plato.Id);
        Assert.Equal([FichaNutricionalEsquema.Identificador(lechuga)], ficha!.Ingredientes!.Select(i => i.AlimentoId));
    }

    [Fact]
    public async Task Sin_ficha_agregar_y_quitar_no_fallan()
    {
        var platoId = Guid.NewGuid();

        await Fichas().AgregarIngredienteAsync(platoId, NuevoAlimento("Tomate"));
        await Fichas().QuitarIngredienteAsync(platoId, Guid.NewGuid());

        Assert.Equal(0, await _contexto.Fichas.CountDocumentsAsync(FilterDefinition<FichaNutricionalDocumento>.Empty));
    }

    // ---- Fallos de MongoDB ----

    [Fact]
    public async Task Un_fallo_de_MongoDB_se_registra_y_no_se_propaga()
    {
        var logger = new LoggerDeTest();
        var almacen = new AlmacenQueLanza();
        var fichas = new FichasNutricionalesEnMongo(almacen, almacen, logger);

        await fichas.EliminarAsync(NuevoAlimento("Avena"));
        await fichas.AgregarIngredienteAsync(Guid.NewGuid(), NuevoAlimento("Tomate"));
        await fichas.QuitarIngredienteAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(3, logger.Advertencias.Count);
        Assert.All(logger.Advertencias, a => Assert.Contains("pendiente de conciliación", a));
    }

    private sealed class AlmacenQueLanza : IFichaNutricionalStore, IDesgloseDeIngredientes
    {
        public Task<bool> EliminarPorOrigenAsync(string tabla, Guid itemId, CancellationToken cancellationToken = default)
            => throw new MongoException("MongoDB no responde.");

        public Task<bool> AgregarIngredienteAsync(Guid platoId, Guid alimentoId, string nombre, CancellationToken cancellationToken = default)
            => throw new MongoException("MongoDB no responde.");

        public Task<bool> QuitarIngredienteAsync(Guid platoId, Guid alimentoId, CancellationToken cancellationToken = default)
            => throw new MongoException("MongoDB no responde.");
    }

    private sealed class LoggerDeTest : ILogger<FichasNutricionalesEnMongo>
    {
        public List<string> Advertencias { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning && exception is not null)
            {
                Advertencias.Add(formatter(state, exception));
            }
        }
    }
}
