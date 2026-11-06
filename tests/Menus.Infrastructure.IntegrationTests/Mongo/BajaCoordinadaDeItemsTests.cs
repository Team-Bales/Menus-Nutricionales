using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Mongo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Mongo;

/// <summary>
/// Pruebas de <see cref="BajaCoordinadaDeItems"/> (RM-09) contra PostgreSQL y MongoDB reales.
/// </summary>
/// <remarks>
/// Corre en la colección <c>PostgreSQLyMongoDB</c>, que levanta los dos contenedores. Cada prueba reinicia
/// PostgreSQL (<see cref="PostgresFixture.ReiniciarAsync"/>) y pide un contexto nuevo de MongoDB
/// (<see cref="MongoFixture.NuevoContextoAsync"/>), así que las fichas de una prueba no interfieren con las de
/// otra.
/// </remarks>
[Collection("PostgreSQLyMongoDB")]
public sealed class BajaCoordinadaDeItemsTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private readonly MongoFixture _mongo;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _sembrador = null!;
    private MenusMongoContext _mongoContexto = null!;

    public BajaCoordinadaDeItemsTests(PostgresFixture postgres, MongoFixture mongo)
    {
        _postgres = postgres;
        _mongo = mongo;
    }

    public async Task InitializeAsync()
    {
        await _postgres.ReiniciarAsync();
        _conexion = await _postgres.AbrirAsync();
        _sembrador = new Sembrador(_conexion);
        (_mongoContexto, _) = await _mongo.NuevoContextoAsync();
    }

    public async Task DisposeAsync() => await _conexion.DisposeAsync();

    private BajaCoordinadaDeItems Servicio(IFichaNutricionalStore? almacen = null, ILogger<BajaCoordinadaDeItems>? logger = null)
        => new(
            _postgres.CrearContexto(),
            almacen ?? new FichaNutricionalStore(_mongoContexto),
            logger ?? new LoggerDeTest());

    private static FichaNutricionalDocumento FichaAlimento(Guid id) => new()
    {
        Origen = new OrigenFicha { Tabla = "Alimento", Id = FichaNutricionalEsquema.Identificador(id) },
        Tipo = "simple",
        ActualizadoEn = DateTime.UtcNow,
    };

    private static FichaNutricionalDocumento FichaPlato(Guid id) => new()
    {
        Origen = new OrigenFicha { Tabla = "Plato", Id = FichaNutricionalEsquema.Identificador(id) },
        Tipo = "compuesto",
        ActualizadoEn = DateTime.UtcNow,
    };

    private async Task<bool> ExisteItemAsync(Guid id)
    {
        using var contexto = _postgres.CrearContexto();
        return await contexto.ItemsAlimentarios.AsNoTracking().AnyAsync(i => i.Id == id);
    }

    private async Task<bool> ExisteComposicionAsync(Guid platoId)
    {
        using var contexto = _postgres.CrearContexto();
        return await contexto.PlatosIngredientes.AsNoTracking().AnyAsync(pi => pi.PlatoId == platoId);
    }

    private async Task<bool> ExisteFichaAsync(string tabla, Guid id)
    {
        var filtro = Builders<FichaNutricionalDocumento>.Filter.And(
            Builders<FichaNutricionalDocumento>.Filter.Eq(f => f.Origen.Tabla, tabla),
            Builders<FichaNutricionalDocumento>.Filter.Eq(f => f.Origen.Id, FichaNutricionalEsquema.Identificador(id)));
        return await _mongoContexto.Fichas.CountDocumentsAsync(filtro) > 0;
    }

    [Fact]
    public async Task Alimento_con_ficha_sin_plato_que_lo_use_se_elimina_junto_con_su_ficha()
    {
        var alimentoId = await _sembrador.AlimentoAsync();
        await _mongoContexto.Fichas.InsertOneAsync(FichaAlimento(alimentoId));

        var resultado = await Servicio().EliminarAsync(alimentoId);

        Assert.Equal(ResultadoBaja.Eliminado, resultado);
        Assert.False(await ExisteItemAsync(alimentoId));
        Assert.False(await ExisteFichaAsync("Alimento", alimentoId));
    }

    [Fact]
    public async Task Plato_con_ficha_se_elimina_junto_con_su_composicion_y_su_ficha()
    {
        var alimentoId = await _sembrador.AlimentoAsync();
        var platoId = await _sembrador.PlatoAsync(alimentoId);
        await _mongoContexto.Fichas.InsertOneAsync(FichaPlato(platoId));

        var resultado = await Servicio().EliminarAsync(platoId);

        Assert.Equal(ResultadoBaja.Eliminado, resultado);
        Assert.False(await ExisteItemAsync(platoId));
        Assert.False(await ExisteComposicionAsync(platoId));
        Assert.False(await ExisteFichaAsync("Plato", platoId));
        Assert.True(await ExisteItemAsync(alimentoId));
    }

    [Fact]
    public async Task Alimento_sin_ficha_se_elimina_sin_lanzar()
    {
        var alimentoId = await _sembrador.AlimentoAsync();

        var resultado = await Servicio().EliminarAsync(alimentoId);

        Assert.Equal(ResultadoBaja.Eliminado, resultado);
        Assert.False(await ExisteItemAsync(alimentoId));
    }

    [Fact]
    public async Task Alimento_ingrediente_de_un_plato_no_se_elimina_RR_04()
    {
        var alimentoId = await _sembrador.AlimentoAsync();
        await _sembrador.PlatoAsync(alimentoId);
        await _mongoContexto.Fichas.InsertOneAsync(FichaAlimento(alimentoId));

        var excepcion = await Assert.ThrowsAnyAsync<Exception>(() => Servicio().EliminarAsync(alimentoId));
        var errorPostgres = excepcion as PostgresException ?? excepcion.InnerException as PostgresException;
        Assert.NotNull(errorPostgres);
        Assert.Equal("23503", errorPostgres!.SqlState);

        Assert.True(await ExisteItemAsync(alimentoId));
        Assert.True(await ExisteFichaAsync("Alimento", alimentoId));
    }

    [Fact]
    public async Task Si_falla_el_borrado_en_Mongo_devuelve_FichaPendiente_y_registra_un_aviso()
    {
        var alimentoId = await _sembrador.AlimentoAsync();
        await _mongoContexto.Fichas.InsertOneAsync(FichaAlimento(alimentoId));
        var logger = new LoggerDeTest();

        var resultado = await Servicio(almacen: new AlmacenQueLanza(), logger: logger).EliminarAsync(alimentoId);

        Assert.Equal(ResultadoBaja.FichaPendiente, resultado);
        Assert.False(await ExisteItemAsync(alimentoId));
        Assert.True(await ExisteFichaAsync("Alimento", alimentoId));
        Assert.Contains(LogLevel.Warning, logger.Niveles);
    }

    [Fact]
    public async Task Id_que_no_existe_devuelve_NoExiste_y_no_toca_una_ficha_huerfana_con_ese_id()
    {
        var idInexistente = Guid.NewGuid();
        await _mongoContexto.Fichas.InsertOneAsync(FichaAlimento(idInexistente));

        var resultado = await Servicio().EliminarAsync(idInexistente);

        Assert.Equal(ResultadoBaja.NoExiste, resultado);
        Assert.True(await ExisteFichaAsync("Alimento", idInexistente));
    }

    [Fact]
    public async Task Al_eliminar_un_item_la_ficha_del_otro_queda_intacta()
    {
        var alimento1 = await _sembrador.AlimentoAsync();
        var alimento2 = await _sembrador.AlimentoAsync();
        await _mongoContexto.Fichas.InsertOneAsync(FichaAlimento(alimento1));
        await _mongoContexto.Fichas.InsertOneAsync(FichaAlimento(alimento2));

        var resultado = await Servicio().EliminarAsync(alimento1);

        Assert.Equal(ResultadoBaja.Eliminado, resultado);
        Assert.False(await ExisteFichaAsync("Alimento", alimento1));
        Assert.True(await ExisteFichaAsync("Alimento", alimento2));
    }

    [Fact]
    public async Task Con_un_CancellationToken_ya_cancelado_no_toca_PostgreSQL_ni_MongoDB()
    {
        var alimentoId = await _sembrador.AlimentoAsync();
        await _mongoContexto.Fichas.InsertOneAsync(FichaAlimento(alimentoId));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Servicio().EliminarAsync(alimentoId, cts.Token));

        Assert.True(await ExisteItemAsync(alimentoId));
        Assert.True(await ExisteFichaAsync("Alimento", alimentoId));
    }

    [Fact]
    public async Task Si_la_cancelacion_llega_en_el_borrado_de_Mongo_se_relanza_y_la_ficha_queda_huerfana()
    {
        var alimentoId = await _sembrador.AlimentoAsync();
        await _mongoContexto.Fichas.InsertOneAsync(FichaAlimento(alimentoId));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Servicio(almacen: new AlmacenCancelado()).EliminarAsync(alimentoId));

        // Regla de orden (documento 2, §3): el borrado de PostgreSQL ya se confirmó y no se deshace; la ficha
        // queda huérfana y la conciliación (RM-10) la elimina más adelante.
        Assert.False(await ExisteItemAsync(alimentoId));
        Assert.True(await ExisteFichaAsync("Alimento", alimentoId));
    }

    private sealed class AlmacenQueLanza : IFichaNutricionalStore
    {
        public Task<bool> EliminarPorOrigenAsync(string tabla, Guid itemId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Mongo no disponible (prueba).");
    }

    /// <summary>Se cancela justo en el paso de MongoDB, con el borrado de PostgreSQL ya confirmado.</summary>
    private sealed class AlmacenCancelado : IFichaNutricionalStore
    {
        public Task<bool> EliminarPorOrigenAsync(string tabla, Guid itemId, CancellationToken cancellationToken = default)
            => throw new OperationCanceledException("Mongo cancelado (prueba).");
    }

    private sealed class LoggerDeTest : ILogger<BajaCoordinadaDeItems>
    {
        public List<LogLevel> Niveles { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Niveles.Add(logLevel);
    }
}
