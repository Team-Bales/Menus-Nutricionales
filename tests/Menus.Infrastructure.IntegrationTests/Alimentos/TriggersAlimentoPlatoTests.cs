using System;
using System.Data;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Alimentos;

/// <summary>
/// Pruebas de los triggers del submodelo Alimento-Plato (#200): RK-03 (todo plato tiene al menos un alimento, comprobado
/// al confirmar), RS-12 (la composición tiene un solo nivel) y RS-13 (un ítem no cambia de clase), más tres pruebas
/// de control. La excepción que lanza un trigger llega como <c>P0001</c>; se identifica el trigger por la función que aparece
/// en <see cref="PostgresException.Where"/>, nunca por el texto del mensaje, que otro issue renumera.
/// </summary>
[Collection("PostgreSQL")]
public sealed class TriggersAlimentoPlatoTests : IAsyncLifetime
{
    private const string RaiseException = "P0001";

    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _sembrador = null!;

    public TriggersAlimentoPlatoTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();
        _conexion = await _fixture.AbrirAsync();
        _sembrador = new Sembrador(_conexion);
    }

    public Task DisposeAsync()
    {
        _conexion?.Dispose();
        return Task.CompletedTask;
    }

    // =========================================================================
    // RK-03: trg_r5_15_plato y trg_r5_15_composicion (fn_r5_15), diferidos
    // =========================================================================

    // 11. Un plato sin composición: el INSERT pasa y el trigger diferido salta al confirmar.
    [Fact]
    public async Task Trg_r5_15_plato_rechaza_en_el_commit_un_plato_sin_alimentos()
    {
        var nutricionistaId = await _sembrador.NutricionistaAsync();
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        await _sembrador.ItemSinValidarAsync("Plato vacío", "Plato", nutricionistaId, tx);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
        AssertTrigger(ex, "fn_r5_15");
    }

    // 12. Quitar el único alimento de un plato: el DELETE pasa y el trigger diferido salta al confirmar.
    [Fact]
    public async Task Trg_r5_15_composicion_rechaza_en_el_commit_un_plato_que_se_queda_sin_alimentos()
    {
        var platoId = await _sembrador.PlatoAsync(await _sembrador.AlimentoAsync());
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        await BorrarComposicionAsync(platoId, tx);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
        AssertTrigger(ex, "fn_r5_15");
    }

    // 12, variante de control: si en la misma transacción se agrega otro alimento, el plato llega al COMMIT con
    // composición y se confirma.
    [Fact]
    public async Task Trg_r5_15_composicion_acepta_cambiar_el_unico_alimento_en_la_misma_transaccion()
    {
        var platoId = await _sembrador.PlatoAsync(await _sembrador.AlimentoAsync());
        var otroAlimento = await _sembrador.AlimentoAsync();
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        await BorrarComposicionAsync(platoId, tx);
        await _sembrador.ComposicionSinValidarAsync(platoId, otroAlimento, transaccion: tx);
        await tx.CommitAsync();

        Assert.Equal(1, await ContarComposicionAsync(platoId));
    }

    // =========================================================================
    // RS-12: trg_r5_18 (fn_r5_18), BEFORE INSERT OR UPDATE en composicion
    // =========================================================================

    // 13. El compuesto tiene que ser un plato: un alimento no se compone de otros alimentos.
    [Fact]
    public async Task Trg_r5_18_rechaza_un_compuesto_que_no_es_un_plato()
    {
        var compuesto = await _sembrador.AlimentoAsync();
        var componente = await _sembrador.AlimentoAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => _sembrador.ComposicionSinValidarAsync(compuesto, componente));

        AssertTrigger(ex, "fn_r5_18");
    }

    // 14. El componente tiene que ser un alimento: un plato no se compone de otros platos.
    [Fact]
    public async Task Trg_r5_18_rechaza_un_componente_que_no_es_un_alimento()
    {
        var plato = await _sembrador.PlatoAsync(await _sembrador.AlimentoAsync());
        var otroPlato = await _sembrador.PlatoAsync(await _sembrador.AlimentoAsync());

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => _sembrador.ComposicionSinValidarAsync(plato, otroPlato));

        AssertTrigger(ex, "fn_r5_18");
    }

    // =========================================================================
    // RS-13: trg_r5_19 (fn_r5_19), BEFORE UPDATE OF clase
    // =========================================================================

    // 15. Un alimento no pasa a ser un plato. El trigger salta antes que el CHECK de la clase, por eso es P0001 y no
    // 23514.
    [Fact]
    public async Task Trg_r5_19_rechaza_cambiar_la_clase_de_un_item()
    {
        var alimento = await _sembrador.AlimentoAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => EjecutarSobreItemAsync("UPDATE items_alimentarios SET clase = 'Plato' WHERE id = @id", alimento));

        AssertTrigger(ex, "fn_r5_19");
    }

    // =========================================================================
    // Controles
    // =========================================================================

    // C1. Un plato con dos alimentos se confirma sin error.
    [Fact]
    public async Task Un_plato_con_dos_alimentos_se_confirma()
    {
        var platoId = await _sembrador.PlatoAsync(await _sembrador.AlimentoAsync(), await _sembrador.AlimentoAsync());

        Assert.Equal(2, await ContarComposicionAsync(platoId));
    }

    // C2. Reasignar la misma clase no dispara trg_r5_19: lo impide su WHEN (NEW.clase IS DISTINCT FROM OLD.clase).
    [Fact]
    public async Task Trg_r5_19_no_salta_si_la_clase_no_cambia()
    {
        var alimento = await _sembrador.AlimentoAsync();

        Assert.Equal(1, await EjecutarSobreItemAsync("UPDATE items_alimentarios SET clase = clase WHERE id = @id", alimento));
    }

    private static void AssertTrigger(PostgresException ex, string funcion)
    {
        Assert.Equal(RaiseException, ex.SqlState);
        Assert.Contains(funcion, ex.Where);
    }

    private async Task BorrarComposicionAsync(Guid platoId, NpgsqlTransaction tx)
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "DELETE FROM composicion WHERE plato_id = @plato_id";
        cmd.Parameters.AddWithValue("plato_id", platoId);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Ejecuta una sentencia con el parámetro <c>@id</c> del ítem y devuelve las filas afectadas.</summary>
    /// <param name="sql">Sentencia SQL de la prueba.</param>
    /// <param name="itemId">El ítem.</param>
    private async Task<int> EjecutarSobreItemAsync(string sql, Guid itemId)
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("id", itemId);
        return await cmd.ExecuteNonQueryAsync();
    }

    private async Task<long> ContarComposicionAsync(Guid platoId)
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM composicion WHERE plato_id = @plato_id";
        cmd.Parameters.AddWithValue("plato_id", platoId);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }
}
