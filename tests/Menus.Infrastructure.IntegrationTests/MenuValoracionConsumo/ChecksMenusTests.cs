using System;
using System.Data;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.MenuValoracionConsumo;

/// <summary>
/// Pruebas de los CHECK del submodelo Menú-Valoración-Consumo (RD-10, RD-11, RD-14, RS-19).
/// Cada prueba verifica que el CHECK rechaza el valor inválido (23514 + ConstraintName) y deja pasar el válido.
/// </summary>
[Collection("PostgreSQL")]
public sealed class ChecksMenusTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _sembrador = null!;

    public ChecksMenusTests(PostgresFixture fixture)
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
    // RD-10: ck_menus_r7_15_cantidad_total_alimentos (InitialCreate.cs:572)
    // =========================================================================

    /// <summary>
    /// Prueba 1: menú con cantidad_total_alimentos = 0 → 23514.
    /// </summary>
    [Fact]
    public async Task Ck_menus_r7_15_cantidad_total_alimentos_rechaza_cero()
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico)
            VALUES (@id, @dieta_id, @nutricionista_creador_id, NOW() - INTERVAL '1 day', 0, FALSE)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("dieta_id", dietaId);
        cmd.Parameters.AddWithValue("nutricionista_creador_id", creadorId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_menus_r7_15_cantidad_total_alimentos", ex.ConstraintName);
    }

    /// <summary>
    /// Control del RD-10: cantidad_total_alimentos = 1 se acepta.
    /// </summary>
    [Fact]
    public async Task Ck_menus_r7_15_cantidad_total_alimentos_acepta_positivo()
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico)
            VALUES (@id, @dieta_id, @nutricionista_creador_id, NOW() - INTERVAL '1 day', 1, FALSE)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("dieta_id", dietaId);
        cmd.Parameters.AddWithValue("nutricionista_creador_id", creadorId);

        await cmd.ExecuteNonQueryAsync(); // No debe lanzar
    }

    // =========================================================================
    // RS-19: ck_menus_r7_37_validacion_origen (InitialCreate.cs:573)
    // =========================================================================

    /// <summary>
    /// Prueba 2: menú con validacion_origen_id = id → 23514. Los triggers BEFORE pasan porque no hay
    /// validación de origen: el CHECK se evalúa antes que la FK.
    /// </summary>
    [Fact]
    public async Task Ck_menus_r7_37_validacion_origen_rechaza_reemplazo_de_si_mismo()
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);

        var menuId = Guid.NewGuid();
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico, validacion_origen_id)
            VALUES (@id, @dieta_id, @nutricionista_creador_id, NOW() - INTERVAL '1 day', 1, FALSE, @validacion_origen_id)
            """;
        cmd.Parameters.AddWithValue("id", menuId);
        cmd.Parameters.AddWithValue("dieta_id", dietaId);
        cmd.Parameters.AddWithValue("nutricionista_creador_id", creadorId);
        cmd.Parameters.AddWithValue("validacion_origen_id", menuId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_menus_r7_37_validacion_origen", ex.ConstraintName);
    }

    // =========================================================================
    // RD-11: ck_menu_distribucion_r7_16_proporcion (InitialCreate.cs:512)
    // =========================================================================

    /// <summary>
    /// Prueba 3: fila en menu_distribucion con proporcion = 0 → 23514; segundo caso con 1.5 → 23514.
    /// El menú y la fila se insertan en la misma transacción: trg_rs_07_distribucion_alta rechaza
    /// ampliar la parametrización de un menú creado en una transacción anterior.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.5)]
    public async Task Ck_menu_distribucion_r7_16_proporcion_rechaza_valor_invalido(double proporcionInvalida)
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);

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
                VALUES (@menu_id, @tipo_preparacion_id, @proporcion)
                """;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("menu_id", menuId);
            // El menú válido usa "Plato fuerte"; la fila nueva usa otro tipo para no chocar con la PK.
            cmd.Parameters.AddWithValue("tipo_preparacion_id", Sembrador.TipoEntrante);
            cmd.Parameters.AddWithValue("proporcion", (decimal)proporcionInvalida);

            var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
            Assert.Equal("23514", ex.SqlState);
            Assert.Equal("ck_menu_distribucion_r7_16_proporcion", ex.ConstraintName);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Control del RD-11: proporcion = 1 y 0.0001 se aceptan.
    /// </summary>
    [Fact]
    public async Task Ck_menu_distribucion_r7_16_proporcion_acepta_valores_validos()
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);

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

            // Dos proporciones válidas: 1 ("Entrante") y 0.0001 ("Postre")
            foreach (var (tipoId, proporcion) in new (Guid, decimal)[]
                     {
                         (Sembrador.TipoEntrante, 1.0000m),
                         (Guid.Parse("55555555-0000-0000-0000-000000000003"), 0.0001m)
                     })
            {
                cmd.CommandText = """
                    INSERT INTO menu_distribucion (menu_id, tipo_preparacion_id, proporcion)
                    VALUES (@menu_id, @tipo_preparacion_id, @proporcion)
                    """;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("menu_id", menuId);
                cmd.Parameters.AddWithValue("tipo_preparacion_id", tipoId);
                cmd.Parameters.AddWithValue("proporcion", proporcion);
                await cmd.ExecuteNonQueryAsync();
            }

            // RK-09 exige también al menos un grupo cubierto para poder confirmar
            cmd.CommandText = """
                INSERT INTO menu_grupo_nutricional (menu_id, grupo_nutricional_id)
                VALUES (@menu_id, @grupo_nutricional_id)
                """;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("grupo_nutricional_id", Sembrador.GrupoCerealesId);
            await cmd.ExecuteNonQueryAsync();

            await tx.CommitAsync(); // No debe lanzar
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // =========================================================================
    // RD-14: ck_resultados_nutricionales_r7_19_nombre y _valor (InitialCreate.cs:93-94)
    // La tabla no la limpia Respawn: toda prueba que la modifica lo hace dentro de una
    // transacción que termina en Rollback.
    // =========================================================================

    /// <summary>
    /// Prueba 4: INSERT de ('Regular', 3) → 23514 con ck_resultados_nutricionales_r7_19_nombre.
    /// </summary>
    [Fact]
    public async Task Ck_resultados_nutricionales_r7_19_nombre_rechaza_nombre_ajeno()
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO resultados_nutricionales (id, nombre, valor)
                VALUES (@id, @nombre, @valor)
                """;
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("nombre", "Regular");
            cmd.Parameters.AddWithValue("valor", 3);

            var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
            Assert.Equal("23514", ex.SqlState);
            Assert.Equal("ck_resultados_nutricionales_r7_19_nombre", ex.ConstraintName);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Prueba 5: INSERT de ('Normal', 6) → 23514 con ck_resultados_nutricionales_r7_19_valor.
    /// El CHECK de la columna falla antes que el índice único de nombre, que también chocaría.
    /// </summary>
    [Fact]
    public async Task Ck_resultados_nutricionales_r7_19_valor_rechaza_valor_6()
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO resultados_nutricionales (id, nombre, valor)
                VALUES (@id, @nombre, @valor)
                """;
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("nombre", "Normal");
            cmd.Parameters.AddWithValue("valor", 6);

            var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
            Assert.Equal("23514", ex.SqlState);
            Assert.Equal("ck_resultados_nutricionales_r7_19_valor", ex.ConstraintName);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
