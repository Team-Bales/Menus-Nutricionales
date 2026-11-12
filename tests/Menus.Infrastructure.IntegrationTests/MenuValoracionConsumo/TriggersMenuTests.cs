using System;
using System.Data;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.MenuValoracionConsumo;

/// <summary>
/// Pruebas de los triggers del submodelo Menú-Valoración-Consumo sobre <c>menus</c>, su parametrización
/// y sus ítems: RK-09 (trg_r7_22), RS-07 (trg_r7_27 y control C1) y el reemplazo (RS-17/RS-18 con su
/// control C5, trg_r7_35_r7_36).
/// </summary>
[Collection("PostgreSQL")]
public sealed class TriggersMenuTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _sembrador = null!;

    public TriggersMenuTests(PostgresFixture fixture)
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

    /// <summary>Dieta con su creador autorizado: lo mínimo para insertar un menú (trg_r6_22_creador).</summary>
    private async Task<(Guid DietaId, Guid CreadorId)> ArmarDietaConCreadorAsync()
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);
        return (dietaId, creadorId);
    }

    // =========================================================================
    // RK-09: trg_r7_22_menu (fn_r7_22), AFTER INSERT en menus, diferido (Sql/R7_22_menu_con_parametros.sql)
    // =========================================================================

    /// <summary>
    /// Prueba 10: transacción: menú con un grupo cubierto y sin distribución; el INSERT pasa y
    /// CommitAsync() lanza P0001 con fn_r7_22.
    /// </summary>
    [Fact]
    public async Task Trg_r7_22_menu_rechaza_menu_sin_distribucion_en_commit()
    {
        var (dietaId, creadorId) = await ArmarDietaConCreadorAsync();

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            var menuId = Guid.NewGuid();
            cmd.CommandText = """
                INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico)
                VALUES (@id, @dieta_id, @nutricionista_creador_id, NOW() - INTERVAL '1 day', 1, FALSE)
                """;
            cmd.Parameters.AddWithValue("id", menuId);
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            cmd.Parameters.AddWithValue("nutricionista_creador_id", creadorId);
            await cmd.ExecuteNonQueryAsync();

            cmd.CommandText = """
                INSERT INTO menu_grupo_nutricional (menu_id, grupo_nutricional_id)
                VALUES (@menu_id, @grupo_nutricional_id)
                """;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("grupo_nutricional_id", Sembrador.GrupoCerealesId);
            await cmd.ExecuteNonQueryAsync();

            // El INSERT pasa; el error sale en el COMMIT
            var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("P0001", ex.SqlState);
            Assert.Contains("fn_r7_22", ex.Where);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Prueba 11: transacción: menú con distribución y sin grupo cubierto → P0001 con fn_r7_22.
    /// </summary>
    [Fact]
    public async Task Trg_r7_22_menu_rechaza_menu_sin_grupos_en_commit()
    {
        var (dietaId, creadorId) = await ArmarDietaConCreadorAsync();

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            var menuId = Guid.NewGuid();
            cmd.CommandText = """
                INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico)
                VALUES (@id, @dieta_id, @nutricionista_creador_id, NOW() - INTERVAL '1 day', 1, FALSE)
                """;
            cmd.Parameters.AddWithValue("id", menuId);
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            cmd.Parameters.AddWithValue("nutricionista_creador_id", creadorId);
            await cmd.ExecuteNonQueryAsync();

            cmd.CommandText = """
                INSERT INTO menu_distribucion (menu_id, tipo_preparacion_id, proporcion)
                VALUES (@menu_id, @tipo_preparacion_id, 1.0)
                """;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("tipo_preparacion_id", Sembrador.TipoPlatoFuerteId);
            await cmd.ExecuteNonQueryAsync();

            var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("P0001", ex.SqlState);
            Assert.Contains("fn_r7_22", ex.Where);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Prueba 12: menú válido (Sembrador.MenuAsync); transacción: DELETE de su única fila de
    /// menu_distribucion; CommitAsync() lanza P0001 con fn_r7_22 (dispara antes que
    /// trg_r7_27_distribucion_borrado por orden alfabético).
    /// </summary>
    [Fact]
    public async Task Trg_r7_22_distribucion_rechaza_delete_de_unica_fila_en_commit()
    {
        var (dietaId, creadorId) = await ArmarDietaConCreadorAsync();
        var menuId = await _sembrador.MenuAsync(dietaId, creadorId);

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM menu_distribucion WHERE menu_id = @menu_id";
            cmd.Parameters.AddWithValue("menu_id", menuId);
            await cmd.ExecuteNonQueryAsync();

            var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("P0001", ex.SqlState);
            Assert.Contains("fn_r7_22", ex.Where);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Prueba 13: igual que la 12 con la única fila de menu_grupo_nutricional → P0001 con fn_r7_22.
    /// </summary>
    [Fact]
    public async Task Trg_r7_22_grupos_rechaza_delete_de_unica_fila_en_commit()
    {
        var (dietaId, creadorId) = await ArmarDietaConCreadorAsync();
        var menuId = await _sembrador.MenuAsync(dietaId, creadorId);

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM menu_grupo_nutricional WHERE menu_id = @menu_id";
            cmd.Parameters.AddWithValue("menu_id", menuId);
            await cmd.ExecuteNonQueryAsync();

            var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("P0001", ex.SqlState);
            Assert.Contains("fn_r7_22", ex.Where);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // =========================================================================
    // RS-07: trg_r7_27_menu (fn_r7_27_menu), BEFORE UPDATE en menus
    // (Sql/R7_27_parametrizacion_inmutable.sql)
    // =========================================================================

    /// <summary>
    /// Prueba 14: UPDATE menus SET cantidad_total_alimentos = cantidad_total_alimentos + 1 →
    /// P0001 con fn_r7_27_menu.
    /// </summary>
    [Fact]
    public async Task Trg_r7_27_menu_rechaza_cambiar_cantidad_total_alimentos()
    {
        var (dietaId, creadorId) = await ArmarDietaConCreadorAsync();
        var menuId = await _sembrador.MenuAsync(dietaId, creadorId);

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = "UPDATE menus SET cantidad_total_alimentos = cantidad_total_alimentos + 1 WHERE id = @menu_id";
        cmd.Parameters.AddWithValue("menu_id", menuId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r7_27_menu", ex.Where);
    }

    /// <summary>
    /// Prueba 15: UPDATE menus SET es_automatico = NOT es_automatico → P0001 con fn_r7_27_menu.
    /// </summary>
    [Fact]
    public async Task Trg_r7_27_menu_rechaza_cambiar_es_automatico()
    {
        var (dietaId, creadorId) = await ArmarDietaConCreadorAsync();
        var menuId = await _sembrador.MenuAsync(dietaId, creadorId);

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = "UPDATE menus SET es_automatico = NOT es_automatico WHERE id = @menu_id";
        cmd.Parameters.AddWithValue("menu_id", menuId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r7_27_menu", ex.Where);
    }

    /// <summary>
    /// Prueba 16: UPDATE menu_distribucion SET proporcion = 0.5 → P0001 con
    /// fn_r7_27_parametros_actualizacion (trg_r7_27_distribucion_actualizacion).
    /// </summary>
    [Fact]
    public async Task Trg_r7_27_distribucion_actualizacion_rechaza_cambiar_proporcion()
    {
        var (dietaId, creadorId) = await ArmarDietaConCreadorAsync();
        var menuId = await _sembrador.MenuAsync(dietaId, creadorId);

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = "UPDATE menu_distribucion SET proporcion = 0.5 WHERE menu_id = @menu_id";
        cmd.Parameters.AddWithValue("menu_id", menuId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r7_27_parametros_actualizacion", ex.Where);
    }

    /// <summary>
    /// Prueba 17: UPDATE menu_grupo_nutricional SET grupo_nutricional_id = otro grupo → P0001 con
    /// fn_r7_27_parametros_actualizacion (trg_r7_27_grupos_actualizacion).
    /// </summary>
    [Fact]
    public async Task Trg_r7_27_grupos_actualizacion_rechaza_cambiar_grupo_cubierto()
    {
        var (dietaId, creadorId) = await ArmarDietaConCreadorAsync();
        var menuId = await _sembrador.MenuAsync(dietaId, creadorId);

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            UPDATE menu_grupo_nutricional SET grupo_nutricional_id = @otro_grupo WHERE menu_id = @menu_id
            """;
        cmd.Parameters.AddWithValue("otro_grupo", Sembrador.GrupoProteinas);
        cmd.Parameters.AddWithValue("menu_id", menuId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r7_27_parametros_actualizacion", ex.Where);
    }

    /// <summary>
    /// Prueba 18: menú con dos filas de menu_distribucion (insertadas junto con el menú); transacción:
    /// DELETE de una; CommitAsync() lanza P0001 con fn_r7_27_parametros_borrado (el menú existe, así que
    /// no es un borrado en cascada).
    /// </summary>
    [Fact]
    public async Task Trg_r7_27_distribucion_borrado_rechaza_delete_de_fila_con_menu_en_commit()
    {
        var (dietaId, creadorId) = await ArmarDietaConCreadorAsync();
        var menuId = await _sembrador.MenuConParametrosAsync(
            dietaId,
            creadorId,
            DateTime.UtcNow.AddHours(-2),
            1,
            false,
            new (Guid TipoPreparacionId, decimal Proporcion)[]
            {
                (Sembrador.TipoPlatoFuerteId, 0.6m),
                (Sembrador.TipoEntrante, 0.4m)
            },
            new[] { Sembrador.GrupoCerealesId });

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                DELETE FROM menu_distribucion WHERE menu_id = @menu_id AND tipo_preparacion_id = @tipo_id
                """;
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("tipo_id", Sembrador.TipoEntrante);
            await cmd.ExecuteNonQueryAsync();

            var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("P0001", ex.SqlState);
            Assert.Contains("fn_r7_27_parametros_borrado", ex.Where);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Prueba 19: igual que la 18 con dos filas de menu_grupo_nutricional → P0001 con
    /// fn_r7_27_parametros_borrado.
    /// </summary>
    [Fact]
    public async Task Trg_r7_27_grupos_borrado_rechaza_delete_de_fila_con_menu_en_commit()
    {
        var (dietaId, creadorId) = await ArmarDietaConCreadorAsync();
        var menuId = await _sembrador.MenuConParametrosAsync(
            dietaId,
            creadorId,
            DateTime.UtcNow.AddHours(-2),
            1,
            false,
            new (Guid TipoPreparacionId, decimal Proporcion)[] { (Sembrador.TipoPlatoFuerteId, 1.0m) },
            new[] { Sembrador.GrupoCerealesId, Sembrador.GrupoProteinas });

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                DELETE FROM menu_grupo_nutricional WHERE menu_id = @menu_id AND grupo_nutricional_id = @grupo_id
                """;
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("grupo_id", Sembrador.GrupoProteinas);
            await cmd.ExecuteNonQueryAsync();

            var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("P0001", ex.SqlState);
            Assert.Contains("fn_r7_27_parametros_borrado", ex.Where);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // =========================================================================
    // RS-07 con RX-02 (control C1, informe línea 415): el borrado del menú entero
    // =========================================================================

    /// <summary>
    /// Prueba 20 (control C1): DELETE FROM menus de un menú sin dictamen borra en cascada
    /// menu_distribucion, menu_grupo_nutricional y menu_item, y el commit pasa.
    /// </summary>
    [Fact]
    public async Task Control_c1_delete_de_menu_sin_dictamen_borra_en_cascada_y_confirma()
    {
        var (dietaId, creadorId) = await ArmarDietaConCreadorAsync();
        var menuId = await _sembrador.MenuAsync(dietaId, creadorId);
        var itemId = await _sembrador.AlimentoConAsync();
        await _sembrador.IncluirEnMenuAsync(menuId, itemId); // el menú no tiene dictamen: se admite

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM menus WHERE id = @menu_id";
            cmd.Parameters.AddWithValue("menu_id", menuId);
            await cmd.ExecuteNonQueryAsync();

            await tx.CommitAsync(); // No debe lanzar: RK-09 y RS-07 dejan pasar el borrado del menú entero
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        // La cascada se llevó también la parametrización y los ítems
        await using var cmdVerificacion = _conexion.CreateCommand();
        cmdVerificacion.CommandText = """
            SELECT (SELECT count(*) FROM menu_distribucion WHERE menu_id = @menu_id)
                 + (SELECT count(*) FROM menu_grupo_nutricional WHERE menu_id = @menu_id)
                 + (SELECT count(*) FROM menu_item WHERE menu_id = @menu_id)
            """;
        cmdVerificacion.Parameters.AddWithValue("menu_id", menuId);
        var filas = Convert.ToInt32(await cmdVerificacion.ExecuteScalarAsync());
        Assert.Equal(0, filas);
    }

    // =========================================================================
    // RS-17 y RS-18: trg_r7_35_r7_36 (fn_r7_35_r7_36), BEFORE INSERT OR UPDATE OF
    // validacion_origen_id, dieta_id en menus (Sql/R7_35_R7_36_menu_de_reemplazo.sql)
    // Orden de disparo BEFORE INSERT/UPDATE en menus: trg_r6_22_creador → trg_r7_27_menu → trg_r7_35_r7_36.
    // =========================================================================

    /// <summary>
    /// Prueba 30: menú de reemplazo cuyo validacion_origen_id es un menú aprobado → P0001 con
    /// fn_r7_35_r7_36 (RS-17: un menú final no se reemplaza).
    /// </summary>
    [Fact]
    public async Task Trg_r7_35_r7_36_rechaza_reemplazo_de_menu_aprobado()
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);
        var revisorId = await _sembrador.NutricionistaAsync();

        var menuAprobadoId = await _sembrador.MenuAprobadoAsync(dietaId, creadorId, revisorId);

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico, validacion_origen_id)
            VALUES (@id, @dieta_id, @nutricionista_creador_id, NOW(), 1, FALSE, @validacion_origen_id)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("dieta_id", dietaId);
        cmd.Parameters.AddWithValue("nutricionista_creador_id", creadorId);
        cmd.Parameters.AddWithValue("validacion_origen_id", menuAprobadoId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r7_35_r7_36", ex.Where);
    }

    /// <summary>
    /// Prueba 31 (RS-18 desde el reemplazo): menú rechazado en la dieta A; reemplazo en la dieta B
    /// (creador autorizado en B, para pasar trg_r6_22_creador) → P0001 con fn_r7_35_r7_36.
    /// </summary>
    [Fact]
    public async Task Trg_r7_35_r7_36_rechaza_reemplazo_de_otra_dieta()
    {
        var dietaA = await _sembrador.DietaAsync();
        var dietaB = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaA);
        await _sembrador.AutorizarAsync(creadorId, dietaB);
        var revisorId = await _sembrador.NutricionistaAsync();

        var menuRechazadoId = await _sembrador.MenuRechazadoAsync(dietaA, creadorId, revisorId);

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico, validacion_origen_id)
            VALUES (@id, @dieta_id, @nutricionista_creador_id, NOW(), 1, FALSE, @validacion_origen_id)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("dieta_id", dietaB);
        cmd.Parameters.AddWithValue("nutricionista_creador_id", creadorId);
        cmd.Parameters.AddWithValue("validacion_origen_id", menuRechazadoId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r7_35_r7_36", ex.Where);
    }

    /// <summary>
    /// Prueba 32 (RS-18 desde el rechazado): menú rechazado con reemplazo en A; UPDATE menus SET
    /// dieta_id = B sobre el rechazado (creador autorizado en B, para pasar trg_r6_22_creador) → P0001
    /// con fn_r7_35_r7_36.
    /// </summary>
    [Fact]
    public async Task Trg_r7_35_r7_36_rechaza_cambiar_dieta_de_menu_reemplazado()
    {
        var dietaA = await _sembrador.DietaAsync();
        var dietaB = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaA);
        await _sembrador.AutorizarAsync(creadorId, dietaB);
        var revisorId = await _sembrador.NutricionistaAsync();

        var menuRechazadoId = await _sembrador.MenuRechazadoAsync(dietaA, creadorId, revisorId);
        await _sembrador.MenuDeReemplazoAsync(dietaA, creadorId, menuRechazadoId); // reemplazo en A

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = "UPDATE menus SET dieta_id = @dieta_b WHERE id = @menu_id";
        cmd.Parameters.AddWithValue("dieta_b", dietaB);
        cmd.Parameters.AddWithValue("menu_id", menuRechazadoId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r7_35_r7_36", ex.Where);
    }

    /// <summary>
    /// Control C5: un reemplazo de la misma dieta de un menú rechazado se inserta y confirma.
    /// </summary>
    [Fact]
    public async Task Control_c5_reemplazo_de_la_misma_dieta_confirma()
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);
        var revisorId = await _sembrador.NutricionistaAsync();

        var menuRechazadoId = await _sembrador.MenuRechazadoAsync(dietaId, creadorId, revisorId);
        var reemplazoId = await _sembrador.MenuDeReemplazoAsync(dietaId, creadorId, menuRechazadoId); // No debe lanzar

        Assert.NotEqual(Guid.Empty, reemplazoId);
    }
}
