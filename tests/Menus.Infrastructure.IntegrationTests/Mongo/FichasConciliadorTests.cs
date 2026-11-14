using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Mongo;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Mongo;

/// <summary>
/// Pruebas de <see cref="FichasConciliador"/> (RM-10) contra PostgreSQL y MongoDB reales.
/// </summary>
/// <remarks>
/// Corre en la colección <c>PostgreSQLyMongoDB</c>, que levanta los dos contenedores. Cada prueba reinicia
/// PostgreSQL (<see cref="PostgresFixture.ReiniciarAsync"/>) y pide un contexto nuevo de MongoDB
/// (<see cref="MongoFixture.NuevoContextoAsync"/>), así que las fichas de una prueba no interfieren con las de
/// otra.
/// </remarks>
[Collection("PostgreSQLyMongoDB")]
public sealed class FichasConciliadorTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private readonly MongoFixture _mongo;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _sembrador = null!;
    private MenusMongoContext _mongoContexto = null!;
    private MenusDbContext _postgresContexto = null!;

    public FichasConciliadorTests(PostgresFixture postgres, MongoFixture mongo)
    {
        _postgres = postgres;
        _mongo = mongo;
    }

    public async Task InitializeAsync()
    {
        await _postgres.ReiniciarAsync();
        _conexion = await _postgres.AbrirAsync();
        _sembrador = new Sembrador(_conexion);
        _postgresContexto = _postgres.CrearContexto();
        (_mongoContexto, _) = await _mongo.NuevoContextoAsync();
    }

    public async Task DisposeAsync() => await _conexion.DisposeAsync();

    private FichasConciliador CrearConciliador(int tamanoLote = 500)
    {
        var opciones = Options.Create(new ConciliacionOptions { TamanoLote = tamanoLote });
        var logger = new LoggerFactory().CreateLogger<FichasConciliador>();
        return new FichasConciliador(_mongoContexto, _postgresContexto, opciones, logger);
    }

    private static FichaNutricionalDocumento FichaAlimento(Guid id, string? nombre = null) => new()
    {
        Origen = new OrigenFicha { Tabla = "Alimento", Id = FichaNutricionalEsquema.Identificador(id) },
        Tipo = "simple",
        ActualizadoEn = DateTime.UtcNow.AddDays(-1),
    };

    private static FichaNutricionalDocumento FichaPlato(Guid id, params (Guid AlimentoId, string? Nombre)[] ingredientes) => new()
    {
        Origen = new OrigenFicha { Tabla = "Plato", Id = FichaNutricionalEsquema.Identificador(id) },
        Tipo = "compuesto",
        Ingredientes = [.. ingredientes.Select(i => new IngredienteFicha
        {
            AlimentoId = FichaNutricionalEsquema.Identificador(i.AlimentoId),
            Nombre = i.Nombre
        })],
        ActualizadoEn = DateTime.UtcNow.AddDays(-1),
    };

    private async Task<FichaNutricionalDocumento?> LeerFichaAsync(string tabla, Guid id)
    {
        var filtro = Builders<FichaNutricionalDocumento>.Filter.And(
            Builders<FichaNutricionalDocumento>.Filter.Eq(f => f.Origen.Tabla, tabla),
            Builders<FichaNutricionalDocumento>.Filter.Eq(f => f.Origen.Id, FichaNutricionalEsquema.Identificador(id)));
        return await _mongoContexto.Fichas.Find(filtro).FirstOrDefaultAsync();
    }

    // ---- Caso 1: Ficha cuyo origen.id no existe en items_alimentarios ----

    [Fact]
    public async Task Ficha_huerfana_sin_item_en_postgres_se_elimina()
    {
        var idInexistente = Guid.NewGuid();
        await _mongoContexto.Fichas.InsertOneAsync(FichaAlimento(idInexistente));

        var resultado = await CrearConciliador().ConciliarAsync();

        Assert.Equal(1, resultado.HuerfanasEliminadas);
        Assert.Null(await LeerFichaAsync("Alimento", idInexistente));
    }

    // ---- Caso 2: Ficha con origen.tabla = "Plato" cuyo ítem es un Alimento ----

    [Fact]
    public async Task Ficha_plato_con_item_alimento_se_elimina_por_clase_distinta()
    {
        var alimentoId = await _sembrador.AlimentoAsync("Alimento Para Plato");
        await _mongoContexto.Fichas.InsertOneAsync(FichaPlato(alimentoId));

        var resultado = await CrearConciliador().ConciliarAsync();

        Assert.Equal(1, resultado.HuerfanasEliminadas);
        Assert.Null(await LeerFichaAsync("Plato", alimentoId));
    }

    // ---- Caso 3: Fichas válidas de alimento y plato se conservan ----

    [Fact]
    public async Task Fichas_validas_se_conservan_contadores_en_cero()
    {
        var alimentoId = await _sembrador.AlimentoAsync("Alimento Valido");
        var platoId = await _sembrador.PlatoAsync(alimentoId);
        await _mongoContexto.Fichas.InsertManyAsync([
            FichaAlimento(alimentoId),
            FichaPlato(platoId)
        ]);

        var resultado = await CrearConciliador().ConciliarAsync();

        Assert.Equal(2, resultado.Revisadas);
        Assert.Equal(0, resultado.HuerfanasEliminadas);
        Assert.Equal(0, resultado.NombresActualizados);
        Assert.Equal(0, resultado.FichasInvalidas);
        Assert.NotNull(await LeerFichaAsync("Alimento", alimentoId));
        Assert.NotNull(await LeerFichaAsync("Plato", platoId));
    }

    // ---- Caso 4: Ficha de plato con ingredientes; se cambió el nombre de A en PostgreSQL ----

    [Fact]
    public async Task Nombre_ingrediente_desactualizado_se_actualiza()
    {
        var alimentoA = await _sembrador.AlimentoAsync("Nombre Original A");
        var alimentoB = await _sembrador.AlimentoAsync("Nombre B");
        var platoId = await _sembrador.PlatoAsync(alimentoA, alimentoB);

        // Insertar ficha con nombres antiguos
        await _mongoContexto.Fichas.InsertOneAsync(FichaPlato(platoId,
            (alimentoA, "Nombre Viejo A"),
            (alimentoB, "Nombre B")));

        // Cambiar nombre en PostgreSQL
        await using (var cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = "UPDATE items_alimentarios SET nombre = @nombre WHERE id = @id";
            cmd.Parameters.AddWithValue("nombre", "Nombre Nuevo A");
            cmd.Parameters.AddWithValue("id", alimentoA);
            await cmd.ExecuteNonQueryAsync();
        }

        var resultado = await CrearConciliador().ConciliarAsync();

        Assert.Equal(1, resultado.NombresActualizados);

        var ficha = await LeerFichaAsync("Plato", platoId);
        Assert.NotNull(ficha);
        var ingA = ficha!.Ingredientes!.First(i => i.AlimentoId == FichaNutricionalEsquema.Identificador(alimentoA));
        Assert.Equal("Nombre Nuevo A", ingA.Nombre);
        var ingB = ficha.Ingredientes!.First(i => i.AlimentoId == FichaNutricionalEsquema.Identificador(alimentoB));
        Assert.Equal("Nombre B", ingB.Nombre); // No cambia
        Assert.True(ficha.ActualizadoEn > DateTime.UtcNow.AddMinutes(-1));
    }

    // ---- Caso 5: Ficha de plato cuyo ingrediente no tiene copia de nombre ----

    [Fact]
    public async Task Ingrediente_sin_copia_nombre_no_se_rellena()
    {
        var alimento = await _sembrador.AlimentoAsync("Alimento Sin Nombre");
        var platoId = await _sembrador.PlatoAsync(alimento);

        // Insertar ficha SIN nombre en el ingrediente
        var ficha = FichaPlato(platoId, (alimento, null));
        ficha.Ingredientes![0].Nombre = null;
        await _mongoContexto.Fichas.InsertOneAsync(ficha);

        var resultado = await CrearConciliador().ConciliarAsync();

        Assert.Equal(0, resultado.NombresActualizados);

        var fichaLeida = await LeerFichaAsync("Plato", platoId);
        Assert.NotNull(fichaLeida);
        var ing = fichaLeida!.Ingredientes!.First();
        Assert.Null(ing.Nombre); // Sigue sin nombre
    }

    // ---- Caso 6: Más fichas que TamanoLote (lote de 2, 5 fichas, 2 huérfanas repartidas) ----

    [Fact]
    public async Task Paginacion_sin_saltos_elimina_exactamente_las_huerfanas()
    {
        var alimento1 = await _sembrador.AlimentoAsync("A1");
        var alimento2 = await _sembrador.AlimentoAsync("A2");
        var alimento3 = await _sembrador.AlimentoAsync("A3");
        var alimento4 = await _sembrador.AlimentoAsync("A4");
        var alimento5 = await _sembrador.AlimentoAsync("A5");

        // 5 fichas: 2 huérfanas (id inexistente) repartidas entre lotes de 2
        await _mongoContexto.Fichas.InsertManyAsync([
            FichaAlimento(Guid.NewGuid()), // huérfana 1
            FichaAlimento(alimento1),
            FichaAlimento(Guid.NewGuid()), // huérfana 2
            FichaAlimento(alimento2),
            FichaAlimento(alimento3),
        ]);

        var resultado = await CrearConciliador(tamanoLote: 2).ConciliarAsync();

        Assert.Equal(5, resultado.Revisadas);
        Assert.Equal(2, resultado.HuerfanasEliminadas);
        Assert.Equal(0, resultado.NombresActualizados);
        Assert.NotNull(await LeerFichaAsync("Alimento", alimento1));
        Assert.NotNull(await LeerFichaAsync("Alimento", alimento2));
        Assert.NotNull(await LeerFichaAsync("Alimento", alimento3));
    }

    // ---- Caso 7: Segunda ejecución inmediata ----

    [Fact]
    public async Task Segunda_ejecucion_inmediata_devuelve_ceros()
    {
        var alimento = await _sembrador.AlimentoAsync("Alimento Idempotente");
        await _mongoContexto.Fichas.InsertOneAsync(FichaAlimento(alimento));

        await CrearConciliador().ConciliarAsync();
        var resultado = await CrearConciliador().ConciliarAsync();

        Assert.Equal(0, resultado.HuerfanasEliminadas);
        Assert.Equal(0, resultado.NombresActualizados);
        Assert.Equal(0, resultado.FichasInvalidas);
    }

    // ---- Caso 8: Ficha con origen.id no parseable (bypass validador) ----

    [Fact]
    public async Task Ficha_con_id_no_parseable_no_se_borra_cuenta_invalidas()
    {
        // Insertar saltando el validador con bypassDocumentValidation
        var fichaInvalida = new FichaNutricionalDocumento
        {
            Origen = new OrigenFicha { Tabla = "Alimento", Id = "no-es-un-guid" },
            Tipo = "simple",
            ActualizadoEn = DateTime.UtcNow.AddDays(-1)
        };
        await _mongoContexto.Fichas.InsertOneAsync(fichaInvalida, new InsertOneOptions { BypassDocumentValidation = true });

        var resultado = await CrearConciliador().ConciliarAsync();

        Assert.Equal(1, resultado.FichasInvalidas);
        Assert.Equal(0, resultado.HuerfanasEliminadas);

        // La ficha sigue ahí
        var filtro = Builders<FichaNutricionalDocumento>.Filter.Eq(f => f.Origen.Id, "no-es-un-guid");
        var count = await _mongoContexto.Fichas.CountDocumentsAsync(filtro);
        Assert.Equal(1, count);
    }

    // ---- Caso 9: Ficha de un ítem borrado con BajaCoordinadaDeItems y fallo simulado en MongoDB (FichaPendiente) ----

    [Fact]
    public async Task Ficha_pendiente_de_baja_coordinada_se_elimina_en_conciliacion()
    {
        // Crear alimento y su ficha
        var alimentoId = await _sembrador.AlimentoAsync("Para Baja Pendiente");
        await _mongoContexto.Fichas.InsertOneAsync(FichaAlimento(alimentoId));

        // Simular baja coordinada que falla en MongoDB -> FichaPendiente
        // (El alimento ya no existe en PostgreSQL, pero la ficha sí en MongoDB)
        await using (var cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM items_alimentarios WHERE id = @id";
            cmd.Parameters.AddWithValue("id", alimentoId);
            await cmd.ExecuteNonQueryAsync();
        }

        var resultado = await CrearConciliador().ConciliarAsync();

        Assert.Equal(1, resultado.HuerfanasEliminadas);
        Assert.Null(await LeerFichaAsync("Alimento", alimentoId));
    }

    // ---- Caso 10: CancellationToken cancelado a mitad ----

    [Fact]
    public async Task Cancelacion_a_mitad_lanza_OperationCanceledException()
    {
        var alimento1 = await _sembrador.AlimentoAsync("A1");
        var alimento2 = await _sembrador.AlimentoAsync("A2");
        await _mongoContexto.Fichas.InsertManyAsync([
            FichaAlimento(alimento1),
            FichaAlimento(alimento2)
        ]);

        using var cts = new CancellationTokenSource();
        // Cancelar inmediatamente
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => CrearConciliador().ConciliarAsync(cts.Token));

        // Las fichas ya procesadas (si las hay) siguen consistentes
        // Aquí solo verificamos que no se corrompió nada
        Assert.NotNull(await LeerFichaAsync("Alimento", alimento1));
        Assert.NotNull(await LeerFichaAsync("Alimento", alimento2));
    }

    // ---- Divergencias RM-06 ----

    [Fact]
    public async Task Divergencia_rm06_se_reporta_sin_corregir()
    {
        var alimentoA = await _sembrador.AlimentoAsync("A");
        var alimentoB = await _sembrador.AlimentoAsync("B");
        var alimentoC = await _sembrador.AlimentoAsync("C");
        var platoId = await _sembrador.PlatoAsync(alimentoA, alimentoB); // Compone: A, B

        // Ficha con ingredientes A, C (diverge de Compone que tiene A, B)
        await _mongoContexto.Fichas.InsertOneAsync(FichaPlato(platoId,
            (alimentoA, "A"),
            (alimentoC, "C")));

        var resultado = await CrearConciliador().ConciliarAsync();

        Assert.Equal(1, resultado.DivergenciasRm06);

        // La ficha NO se corrige (solo se reporta)
        var ficha = await LeerFichaAsync("Plato", platoId);
        Assert.NotNull(ficha);
        Assert.Equal(2, ficha!.Ingredientes!.Count);
        Assert.Contains(ficha.Ingredientes, i => i.AlimentoId == FichaNutricionalEsquema.Identificador(alimentoC));
    }

    // ---- actualizadoEn solo avanza cuando hay cambios ----

    [Fact]
    public async Task ActualizadoEn_no_cambia_si_no_hay_actualizaciones()
    {
        var alimento = await _sembrador.AlimentoAsync("Sin Cambios");
        var platoId = await _sembrador.PlatoAsync(alimento);
        await _mongoContexto.Fichas.InsertOneAsync(FichaPlato(platoId, (alimento, "Sin Cambios")));

        var antes = (await LeerFichaAsync("Plato", platoId))!.ActualizadoEn;
        await Task.Delay(10); // Asegurar que pasaría tiempo
        await CrearConciliador().ConciliarAsync();
        var despues = (await LeerFichaAsync("Plato", platoId))!.ActualizadoEn;

        Assert.Equal(antes, despues);
    }
}
