using System;
using System.Data;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.MenuValoracionConsumo;

/// <summary>
/// Pruebas de los triggers sobre <c>asignacion</c>, <c>servicios</c> y <c>revalorizaciones</c>:
/// RS-16 con trg_r7_34 (asignación de menú aprobado), RS-14 con trg_r7_31 (servicio tras la aprobación)
/// y RS-10 con trg_r7_30 (valoración generada por una revalorización).
/// </summary>
[Collection("PostgreSQL")]
public sealed class TriggersServicioAsignacionTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _sembrador = null!;

    public TriggersServicioAsignacionTests(PostgresFixture fixture)
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

    /// <summary>Dieta con creador autorizado, revisor distinto y paciente inscrito: lo que exige una asignación.</summary>
    private async Task<(Guid DietaId, Guid CreadorId, Guid RevisorId, Guid PacienteId)> ArmarDietaAsync()
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);
        var revisorId = await _sembrador.NutricionistaAsync();
        var pacienteId = await _sembrador.PacienteAsync();
        await _sembrador.InscribirAsync(pacienteId, dietaId); // trg_r6_23 exige inscripción
        return (dietaId, creadorId, revisorId, pacienteId);
    }

    /// <summary>Registra una asignación dentro de una transacción propia, para dejarle el commit a la prueba.</summary>
    private async Task AsignarEnTransaccionAsync(Guid menuId, Guid pacienteId)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO asignacion (menu_id, paciente_id)
                VALUES (@menu_id, @paciente_id)
                """;
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("paciente_id", pacienteId);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
    }

    /// <summary>Registra un servicio dentro de una transacción propia, para dejarle el commit a la prueba.</summary>
    private async Task ServirEnTransaccionAsync(Guid menuId, Guid pacienteId, DateOnly fechaServicio)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO servicios (menu_id, paciente_id, fecha_servicio)
                VALUES (@menu_id, @paciente_id, @fecha_servicio)
                """;
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("paciente_id", pacienteId);
            cmd.Parameters.AddWithValue("fecha_servicio", fechaServicio.ToDateTime(TimeOnly.MinValue));
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
    // RS-16: trg_r7_34 (fn_r7_34), AFTER INSERT OR UPDATE en asignacion, diferido
    // (Sql/R7_34_asignacion_de_menu_aprobado.sql)
    // =========================================================================

    /// <summary>
    /// Prueba 27: paciente inscrito; menú pendiente (sin validación); transacción: INSERT INTO asignacion;
    /// el INSERT pasa y CommitAsync() lanza P0001 con fn_r7_34.
    /// </summary>
    [Fact]
    public async Task Trg_r7_34_rechaza_asignacion_de_menu_pendiente_en_commit()
    {
        var (dietaId, creadorId, _, pacienteId) = await ArmarDietaAsync();
        var menuId = await _sembrador.MenuAsync(dietaId, creadorId); // pendiente

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO asignacion (menu_id, paciente_id)
                VALUES (@menu_id, @paciente_id)
                """;
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("paciente_id", pacienteId);
            await cmd.ExecuteNonQueryAsync(); // El INSERT pasa (trg_r6_23 ve al paciente inscrito)

            var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("P0001", ex.SqlState);
            Assert.Contains("fn_r7_34", ex.Where);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Prueba 28: igual con un menú rechazado (aprobado = false) → P0001 con fn_r7_34.
    /// </summary>
    [Fact]
    public async Task Trg_r7_34_rechaza_asignacion_de_menu_rechazado_en_commit()
    {
        var (dietaId, creadorId, revisorId, pacienteId) = await ArmarDietaAsync();
        var menuRechazadoId = await _sembrador.MenuRechazadoAsync(dietaId, creadorId, revisorId);

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO asignacion (menu_id, paciente_id)
                VALUES (@menu_id, @paciente_id)
                """;
            cmd.Parameters.AddWithValue("menu_id", menuRechazadoId);
            cmd.Parameters.AddWithValue("paciente_id", pacienteId);
            await cmd.ExecuteNonQueryAsync();

            var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("P0001", ex.SqlState);
            Assert.Contains("fn_r7_34", ex.Where);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Control C3: con el menú aprobado, la asignación se confirma.
    /// </summary>
    [Fact]
    public async Task Control_c3_asignacion_de_menu_aprobado_confirma()
    {
        var (dietaId, creadorId, revisorId, pacienteId) = await ArmarDietaAsync();
        var menuId = await _sembrador.MenuAprobadoAsync(dietaId, creadorId, revisorId);

        await AsignarEnTransaccionAsync(menuId, pacienteId); // No debe lanzar
    }

    // =========================================================================
    // RS-14: trg_r7_31 (fn_r7_31), AFTER INSERT OR UPDATE en servicios, diferido
    // (Sql/R7_31_servicio_tras_aprobacion.sql)
    // =========================================================================

    /// <summary>
    /// Prueba 29: menú aprobado el día D y asignado; INSERT INTO servicios con fecha_servicio = D - 1;
    /// CommitAsync() lanza P0001 con fn_r7_31.
    /// </summary>
    [Fact]
    public async Task Trg_r7_31_rechaza_servicio_anterior_a_la_aprobacion_en_commit()
    {
        var (dietaId, creadorId, revisorId, pacienteId) = await ArmarDietaAsync();

        // Aprobación fijada al inicio del día D (UTC): la regla compara días, y así la prueba no
        // depende de la hora a la que corra (la regla compara (fecha_validacion AT TIME ZONE 'UTC')::date)
        var diaDeAprobacion = DateTime.UtcNow.Date;
        var menuId = await _sembrador.MenuAprobadoAsync(dietaId, creadorId, revisorId, diaDeAprobacion);
        await _sembrador.AsignarAsync(menuId, pacienteId);

        var diaAnterior = DateOnly.FromDateTime(diaDeAprobacion).AddDays(-1);
        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => ServirEnTransaccionAsync(menuId, pacienteId, diaAnterior));
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r7_31", ex.Where);
    }

    /// <summary>
    /// Control C4: con fecha_servicio = D (la regla compara días UTC), el servicio se confirma.
    /// </summary>
    [Fact]
    public async Task Control_c4_servicio_en_el_dia_de_aprobacion_confirma()
    {
        var (dietaId, creadorId, revisorId, pacienteId) = await ArmarDietaAsync();

        var diaDeAprobacion = DateTime.UtcNow.Date;
        var menuId = await _sembrador.MenuAprobadoAsync(dietaId, creadorId, revisorId, diaDeAprobacion);
        await _sembrador.AsignarAsync(menuId, pacienteId);

        await ServirEnTransaccionAsync(menuId, pacienteId, DateOnly.FromDateTime(diaDeAprobacion)); // No debe lanzar
    }

    // =========================================================================
    // RS-10: trg_r7_30 (fn_r7_30), BEFORE INSERT OR UPDATE en revalorizaciones
    // (Sql/R7_30_valoracion_generada.sql)
    // =========================================================================

    /// <summary>
    /// Prueba 33 (RS-10): valoración generada de otra asignación que la revisada → P0001 con fn_r7_30.
    /// </summary>
    [Fact]
    public async Task Trg_r7_30_rechaza_valoracion_generada_de_otra_asignacion()
    {
        var (dietaId, creadorId, revisorId, pacienteId) = await ArmarDietaAsync();

        var menu1 = await _sembrador.MenuAprobadoAsync(dietaId, creadorId, revisorId);
        var menu2 = await _sembrador.MenuAprobadoAsync(dietaId, creadorId, revisorId);
        await _sembrador.AsignarAsync(menu1, pacienteId);
        await _sembrador.AsignarAsync(menu2, pacienteId); // segunda asignación: otra valoración posible

        var ejecutorId = await _sembrador.NutricionistaAsync();
        var valoracionRevisadaId = await _sembrador.ValorarAsync(menu1, pacienteId, ejecutorId);
        var valoracionGeneradaId = await _sembrador.ValorarAsync(menu2, pacienteId, ejecutorId); // otra asignación

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => _sembrador.RevalorizarAsync(valoracionRevisadaId, ejecutorId, valoracionGeneradaId));
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r7_30", ex.Where);
    }

    /// <summary>
    /// Prueba 34 (RS-10): valoración generada de la misma asignación pero emitida por un nutricionista
    /// distinto del ejecutor → P0001 con fn_r7_30.
    /// </summary>
    [Fact]
    public async Task Trg_r7_30_rechaza_valoracion_generada_por_otro_nutricionista()
    {
        var (dietaId, creadorId, revisorId, pacienteId) = await ArmarDietaAsync();

        var menuId = await _sembrador.MenuAprobadoAsync(dietaId, creadorId, revisorId);
        await _sembrador.AsignarAsync(menuId, pacienteId);

        var ejecutorId = await _sembrador.NutricionistaAsync();
        var otroNutricionistaId = await _sembrador.NutricionistaAsync();

        var valoracionRevisadaId = await _sembrador.ValorarAsync(menuId, pacienteId, ejecutorId);
        var valoracionGeneradaId = await _sembrador.ValorarAsync(menuId, pacienteId, otroNutricionistaId);

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => _sembrador.RevalorizarAsync(valoracionRevisadaId, ejecutorId, valoracionGeneradaId));
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r7_30", ex.Where);
    }

    /// <summary>
    /// Control C6: misma asignación y mismo ejecutor, la revalorización entra.
    /// </summary>
    [Fact]
    public async Task Control_c6_revalorizacion_coherente_acepta()
    {
        var (dietaId, creadorId, revisorId, pacienteId) = await ArmarDietaAsync();

        var menuId = await _sembrador.MenuAprobadoAsync(dietaId, creadorId, revisorId);
        await _sembrador.AsignarAsync(menuId, pacienteId);

        var ejecutorId = await _sembrador.NutricionistaAsync();
        var valoracionRevisadaId = await _sembrador.ValorarAsync(menuId, pacienteId, ejecutorId);
        var valoracionGeneradaId = await _sembrador.ValorarAsync(menuId, pacienteId, ejecutorId);

        var revalorizacionId = await _sembrador.RevalorizarAsync(valoracionRevisadaId, ejecutorId, valoracionGeneradaId); // No debe lanzar

        Assert.NotEqual(Guid.Empty, revalorizacionId);
    }
}
