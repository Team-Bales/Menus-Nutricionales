using System;
using System.Data;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.MenuValoracionConsumo;

/// <summary>
/// Pruebas de los índices únicos del submodelo Menú-Valoración-Consumo (RD-14, RK-11, RK-15).
/// Cada prueba verifica que el índice único rechaza el duplicado (23505 + ConstraintName).
/// </summary>
[Collection("PostgreSQL")]
public sealed class IndicesUnicosMenusTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _sembrador = null!;

    public IndicesUnicosMenusTests(PostgresFixture fixture)
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
    // RK-15: ux_menus_r7_38_validacion_origen (InitialCreate.cs:785)
    // Una validación origina a lo sumo un menú de reemplazo.
    // =========================================================================

    /// <summary>
    /// Prueba 6: un menú rechazado y dos menús de reemplazo con validacion_origen_id = ese menú,
    /// de la misma dieta: el segundo → 23505.
    /// </summary>
    [Fact]
    public async Task Ux_menus_r7_38_validacion_origen_rechaza_segundo_reemplazo()
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);
        var revisorId = await _sembrador.NutricionistaAsync();

        var menuRechazadoId = await _sembrador.MenuRechazadoAsync(dietaId, creadorId, revisorId);
        await _sembrador.MenuDeReemplazoAsync(dietaId, creadorId, menuRechazadoId); // primer reemplazo: consume el índice

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico, validacion_origen_id)
            VALUES (@id, @dieta_id, @nutricionista_creador_id, NOW(), 1, FALSE, @validacion_origen_id)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("dieta_id", dietaId);
        cmd.Parameters.AddWithValue("nutricionista_creador_id", creadorId);
        cmd.Parameters.AddWithValue("validacion_origen_id", menuRechazadoId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23505", ex.SqlState);
        Assert.Equal("ux_menus_r7_38_validacion_origen", ex.ConstraintName);
    }

    // =========================================================================
    // RD-14: IX_resultados_nutricionales_nombre (InitialCreate.cs:829)
    // y IX_resultados_nutricionales_valor (InitialCreate.cs:835)
    // La tabla no la limpia Respawn: toda prueba que la modifica lo hace dentro de una
    // transacción que termina en Rollback.
    // =========================================================================

    /// <summary>
    /// Prueba 7: borrar 'Muy mala' e INSERT de ('Normal', 1) (valor libre, nombre repetido) → 23505
    /// con IX_resultados_nutricionales_nombre. Borrar 'Muy mala' libera el valor 1 para aislar el
    /// índice de nombre, porque los CHECK no ligan nombre y valor.
    /// </summary>
    [Fact]
    public async Task IX_resultados_nutricionales_nombre_rechaza_nombre_duplicado()
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM resultados_nutricionales WHERE nombre = 'Muy mala'";
            await cmd.ExecuteNonQueryAsync();

            cmd.CommandText = """
                INSERT INTO resultados_nutricionales (id, nombre, valor)
                VALUES (@id, @nombre, @valor)
                """;
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("nombre", "Normal");
            cmd.Parameters.AddWithValue("valor", 1);

            var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
            Assert.Equal("23505", ex.SqlState);
            Assert.Equal("IX_resultados_nutricionales_nombre", ex.ConstraintName);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Prueba 8: borrar 'Muy mala' e INSERT de ('Muy mala', 3) (nombre libre, valor repetido) → 23505
    /// con IX_resultados_nutricionales_valor.
    /// </summary>
    [Fact]
    public async Task IX_resultados_nutricionales_valor_rechaza_valor_duplicado()
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM resultados_nutricionales WHERE nombre = 'Muy mala'";
            await cmd.ExecuteNonQueryAsync();

            cmd.CommandText = """
                INSERT INTO resultados_nutricionales (id, nombre, valor)
                VALUES (@id, @nombre, @valor)
                """;
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("nombre", "Muy mala");
            cmd.Parameters.AddWithValue("valor", 3);

            var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
            Assert.Equal("23505", ex.SqlState);
            Assert.Equal("IX_resultados_nutricionales_valor", ex.ConstraintName);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // =========================================================================
    // RK-11: IX_revalorizaciones_valoracion_generada_id (InitialCreate.cs:846)
    // Cada valoración es generada por a lo sumo una revalorización.
    // =========================================================================

    /// <summary>
    /// Prueba 9: tres valoraciones V1, V2, V3 de la misma asignación (V2 emitida por el ejecutor E);
    /// R1 (revisada V1, generada V2, ejecutor E) y R2 (revisada V3, generada V2, ejecutor E): R2 → 23505.
    /// </summary>
    [Fact]
    public async Task IX_revalorizaciones_valoracion_generada_id_rechaza_valoracion_generada_repetida()
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);
        var revisorId = await _sembrador.NutricionistaAsync();
        var pacienteId = await _sembrador.PacienteAsync();
        await _sembrador.InscribirAsync(pacienteId, dietaId);

        var menuId = await _sembrador.MenuAprobadoAsync(dietaId, creadorId, revisorId);
        await _sembrador.AsignarAsync(menuId, pacienteId);

        var ejecutorId = await _sembrador.NutricionistaAsync();
        var otroNutricionistaId = await _sembrador.NutricionistaAsync();

        var v1 = await _sembrador.ValorarAsync(menuId, pacienteId, otroNutricionistaId);
        var v2 = await _sembrador.ValorarAsync(menuId, pacienteId, ejecutorId);
        var v3 = await _sembrador.ValorarAsync(menuId, pacienteId, otroNutricionistaId);

        await _sembrador.RevalorizarAsync(v1, ejecutorId, v2); // R1: consume el índice

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO revalorizaciones (id, fecha_solicitud, nutricionista_id, valoracion_revisada_id, valoracion_generada_id)
            VALUES (@id, @fecha_solicitud, @nutricionista_id, @valoracion_revisada_id, @valoracion_generada_id)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("fecha_solicitud", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("nutricionista_id", ejecutorId);
        cmd.Parameters.AddWithValue("valoracion_revisada_id", v3);
        cmd.Parameters.AddWithValue("valoracion_generada_id", v2);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23505", ex.SqlState);
        Assert.Equal("IX_revalorizaciones_valoracion_generada_id", ex.ConstraintName);
    }
}
