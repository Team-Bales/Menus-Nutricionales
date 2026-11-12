using System;
using System.Data;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.MenuValoracionConsumo;

/// <summary>
/// Pruebas de los triggers sobre <c>validacion</c>: RS-03 con trg_rs_03 (revisor jefe), RS-03/RS-05/RS-06
/// con trg_validacion y RS-15 con trg_r7_33 (validación inmutable).
/// </summary>
/// <remarks>
/// Orden de disparo: en <c>BEFORE INSERT</c> de <c>validacion</c>, primero <c>trg_rs_03</c> (exige
/// <c>es_jefe</c>) y después <c>trg_validacion</c>; en <c>BEFORE UPDATE</c>, <c>trg_r7_33</c> antes que
/// <c>trg_validacion</c>. Para probar <c>fn_validacion</c> el revisor debe ser jefe, o <c>trg_rs_03</c>
/// salta antes.
/// </remarks>
[Collection("PostgreSQL")]
public sealed class TriggersValidacionTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _sembrador = null!;

    public TriggersValidacionTests(PostgresFixture fixture)
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

    /// <summary>Dieta con su creador autorizado y un menú pendiente de dictamen.</summary>
    private async Task<(Guid DietaId, Guid CreadorId, Guid MenuId)> ArmarMenuPendienteAsync()
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);
        var menuId = await _sembrador.MenuAsync(dietaId, creadorId); // fecha_creacion = NOW() - 1 day
        return (dietaId, creadorId, menuId);
    }

    /// <summary>Inserta una fila en validacion con la fecha dada, dentro de una transacción propia.</summary>
    private async Task InsertarValidacionAsync(Guid menuId, Guid revisorId, DateTime fechaValidacion)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO validacion (menu_id, nutricionista_id, fecha_validacion, aprobado)
                VALUES (@menu_id, @nutricionista_id, @fecha_validacion, TRUE)
                """;
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("nutricionista_id", revisorId);
            cmd.Parameters.AddWithValue("fecha_validacion", fechaValidacion);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
    }

    // =========================================================================
    // RS-03: trg_rs_03 (fn_rs_03), BEFORE INSERT en validacion (Sql/RS_03_revisor_jefe.sql)
    // =========================================================================

    /// <summary>
    /// Prueba 21: revisor autorizado en la dieta, distinto del creador, con es_jefe = false → P0001 con
    /// fn_rs_03 (dispara antes que trg_validacion por orden alfabético).
    /// </summary>
    [Fact]
    public async Task Trg_rs_03_rechaza_revisor_que_no_es_jefe()
    {
        var (dietaId, _, menuId) = await ArmarMenuPendienteAsync();
        var revisorId = await _sembrador.NutricionistaAsync(); // no jefe
        await _sembrador.AutorizarAsync(revisorId, dietaId);

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => InsertarValidacionAsync(menuId, revisorId, DateTime.UtcNow));
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_rs_03", ex.Where);
    }

    // =========================================================================
    // RS-03 (autorizado): trg_validacion (fn_validacion), BEFORE INSERT OR UPDATE en validacion
    // (Sql/R6_22_R7_25_R7_26_validacion.sql)
    // =========================================================================

    /// <summary>
    /// Prueba 22: revisor jefe, distinto del creador, sin fila en autoriza para la dieta → P0001 con
    /// fn_validacion.
    /// </summary>
    [Fact]
    public async Task Trg_validacion_rechaza_revisor_jefe_no_autorizado()
    {
        var (_, _, menuId) = await ArmarMenuPendienteAsync();
        var revisorId = await _sembrador.NutricionistaAsync();
        await _sembrador.HacerJefeAsync(revisorId); // jefe, pero sin autoriza

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => InsertarValidacionAsync(menuId, revisorId, DateTime.UtcNow));
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_validacion", ex.Where);
    }

    /// <summary>
    /// Prueba 23 (RS-05): el creador del menú (jefe y autorizado) intenta validarlo → P0001 con
    /// fn_validacion.
    /// </summary>
    [Fact]
    public async Task Trg_validacion_rechaza_dictamen_del_propio_creador()
    {
        var (dietaId, creadorId, menuId) = await ArmarMenuPendienteAsync();
        await _sembrador.HacerJefeAsync(creadorId); // jefe y autorizado: pasa trg_rs_03 y salta a fn_validacion

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => InsertarValidacionAsync(menuId, creadorId, DateTime.UtcNow));
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_validacion", ex.Where);
    }

    /// <summary>
    /// Prueba 24 (RS-06): revisor jefe, autorizado y distinto del creador, con fecha_validacion anterior
    /// a menus.fecha_creacion → P0001 con fn_validacion.
    /// </summary>
    [Fact]
    public async Task Trg_validacion_rechaza_validacion_anterior_a_la_creacion()
    {
        var (dietaId, creadorId, menuId) = await ArmarMenuPendienteAsync();
        var revisorId = await _sembrador.NutricionistaAsync();
        await _sembrador.HacerJefeAsync(revisorId);
        await _sembrador.AutorizarAsync(revisorId, dietaId);

        // El menú se creó con NOW() - 1 day; la validación es anterior: NOW() - 2 days
        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => InsertarValidacionAsync(menuId, revisorId, DateTime.UtcNow.AddDays(-2)));
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_validacion", ex.Where);
    }

    /// <summary>
    /// Control C2: con la fecha posterior a la creación, la validación entra.
    /// </summary>
    [Fact]
    public async Task Control_c2_validacion_con_fecha_posterior_a_la_creacion_acepta()
    {
        var (dietaId, creadorId, menuId) = await ArmarMenuPendienteAsync();
        var revisorId = await _sembrador.NutricionistaAsync();
        await _sembrador.HacerJefeAsync(revisorId);
        await _sembrador.AutorizarAsync(revisorId, dietaId);

        await _sembrador.ValidarAsync(menuId, revisorId, aprobado: true, DateTime.UtcNow); // No debe lanzar
    }

    // =========================================================================
    // RS-15: trg_r7_33 (fn_r7_33), BEFORE UPDATE OR DELETE en validacion
    // (Sql/R7_33_validacion_inmutable.sql)
    // =========================================================================

    /// <summary>
    /// Prueba 25 (RS-15): UPDATE validacion SET observaciones = 'x' → P0001 con fn_r7_33 (trg_r7_33
    /// dispara antes que trg_validacion por orden alfabético).
    /// </summary>
    [Fact]
    public async Task Trg_r7_33_rechaza_update_de_la_validacion()
    {
        var (dietaId, creadorId, menuId) = await ArmarMenuPendienteAsync();
        var revisorId = await _sembrador.NutricionistaAsync();
        await _sembrador.MenuAprobadoAsync(dietaId, creadorId, revisorId); // deja la validación registrada

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = "UPDATE validacion SET observaciones = 'x' WHERE menu_id = @menu_id";
        cmd.Parameters.AddWithValue("menu_id", menuId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r7_33", ex.Where);
    }

    /// <summary>
    /// Prueba 26 (RS-15): DELETE FROM validacion → P0001 con fn_r7_33.
    /// </summary>
    [Fact]
    public async Task Trg_r7_33_rechaza_delete_de_la_validacion()
    {
        var (dietaId, creadorId, menuId) = await ArmarMenuPendienteAsync();
        var revisorId = await _sembrador.NutricionistaAsync();
        await _sembrador.MenuAprobadoAsync(dietaId, creadorId, revisorId);

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = "DELETE FROM validacion WHERE menu_id = @menu_id";
        cmd.Parameters.AddWithValue("menu_id", menuId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r7_33", ex.Where);
    }
}
