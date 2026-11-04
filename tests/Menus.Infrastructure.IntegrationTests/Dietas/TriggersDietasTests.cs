using System;
using System.Data;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Dietas;

/// <summary>
/// Pruebas de los triggers del submodelo Dieta-Nutricionista-Paciente-Sala (RK-07, RS-03, RS-04, RS-11).
/// </summary>
[Collection("PostgreSQL")]
public sealed class TriggersDietasTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _sembrador = null!;

    public TriggersDietasTests(PostgresFixture fixture)
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
    // RK-07: trg_r6_21_dieta (fn_r6_21) - AFTER INSERT en dietas, diferido
    // =========================================================================

    /// <summary>
    /// Prueba 13: INSERT INTO dietas sin ninguna fila en cubre_dieta; el INSERT pasa
    /// y CommitAsync() lanza P0001 con fn_r6_21.
    /// </summary>
    [Fact]
    public async Task Trg_r6_21_dieta_rechaza_dieta_sin_cubre_dieta_en_commit()
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            var dietaId = Guid.NewGuid();
            cmd.CommandText = """
                INSERT INTO dietas (id, nombre, programa_atencion_id)
                VALUES (@id, @nombre, @programa_atencion_id)
                """;
            cmd.Parameters.AddWithValue("id", dietaId);
            cmd.Parameters.AddWithValue("nombre", "Dieta sin cobertura");
            cmd.Parameters.AddWithValue("programa_atencion_id", Guid.Parse("33333333-0000-0000-0000-000000000001"));
            await cmd.ExecuteNonQueryAsync();

            // El INSERT pasa, el error sale en el COMMIT
            var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("P0001", ex.SqlState);
            Assert.Contains("fn_r6_21", ex.Where);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Prueba 14: dieta + una fila de cubre_dieta con tipo = 'Restringido' (ninguna Requerido);
    /// CommitAsync() lanza P0001.
    /// </summary>
    [Fact]
    public async Task Trg_r6_21_dieta_rechaza_solo_restringido_en_commit()
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            var dietaId = Guid.NewGuid();
            cmd.CommandText = """
                INSERT INTO dietas (id, nombre, programa_atencion_id)
                VALUES (@id, @nombre, @programa_atencion_id)
                """;
            cmd.Parameters.AddWithValue("id", dietaId);
            cmd.Parameters.AddWithValue("nombre", "Dieta solo restringido");
            cmd.Parameters.AddWithValue("programa_atencion_id", Guid.Parse("33333333-0000-0000-0000-000000000001"));
            await cmd.ExecuteNonQueryAsync();

            cmd.CommandText = """
                INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo)
                VALUES (@dieta_id, @grupo_id, 'Restringido')
                """;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            cmd.Parameters.AddWithValue("grupo_id", Guid.Parse("11111111-0000-0000-0000-000000000001")); // Cereales
            await cmd.ExecuteNonQueryAsync();

            var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("P0001", ex.SqlState);
            Assert.Contains("fn_r6_21", ex.Where);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // =========================================================================
    // RK-07: trg_r6_21_cubre (fn_r6_21) - AFTER DELETE OR UPDATE en cubre_dieta, diferido
    // =========================================================================

    /// <summary>
    /// Prueba 15: dieta válida (Sembrador.DietaAsync()); transacción: DELETE de su única fila Requerido;
    /// CommitAsync() lanza P0001.
    /// </summary>
    [Fact]
    public async Task Trg_r6_21_cubre_rechaza_delete_de_unico_requerido_en_commit()
    {
        var dietaId = await _sembrador.DietaAsync(); // crea dieta + cubre_dieta Requerido (Cereales)

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                DELETE FROM cubre_dieta WHERE dieta_id = @dieta_id AND tipo = 'Requerido'
                """;
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            await cmd.ExecuteNonQueryAsync();

            var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("P0001", ex.SqlState);
            Assert.Contains("fn_r6_21", ex.Where);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Prueba 16: dieta válida; transacción: UPDATE cubre_dieta SET tipo = 'Restringido';
    /// CommitAsync() lanza P0001. Control (C1): en la misma transacción, además del UPDATE,
    /// insertar otro grupo Requerido; el commit pasa.
    /// </summary>
    [Fact]
    public async Task Trg_r6_21_cubre_rechaza_update_a_restringido_sin_otro_requerido_en_commit()
    {
        var dietaId = await _sembrador.DietaAsync(); // crea dieta + cubre_dieta Requerido (Cereales)

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                UPDATE cubre_dieta SET tipo = 'Restringido' WHERE dieta_id = @dieta_id
                """;
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            await cmd.ExecuteNonQueryAsync();

            var ex = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("P0001", ex.SqlState);
            Assert.Contains("fn_r6_21", ex.Where);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Control C1: UPDATE a Restringido PERO insertando otro grupo Requerido en la misma transacción; commit pasa.
    /// </summary>
    [Fact]
    public async Task Trg_r6_21_cubre_acepta_update_si_hay_otro_requerido_en_misma_transaccion()
    {
        var dietaId = await _sembrador.DietaAsync(); // crea dieta + cubre_dieta Requerido (Cereales)

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;

            // Cambia Cereales a Restringido
            cmd.CommandText = """
                UPDATE cubre_dieta SET tipo = 'Restringido' WHERE dieta_id = @dieta_id
                """;
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            await cmd.ExecuteNonQueryAsync();

            // Añade otro grupo Requerido (Proteínas)
            cmd.CommandText = """
                INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo)
                VALUES (@dieta_id, @grupo_id, 'Requerido')
                """;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            cmd.Parameters.AddWithValue("grupo_id", Guid.Parse("11111111-0000-0000-0000-000000000002")); // Proteínas
            await cmd.ExecuteNonQueryAsync();

            // Commit debe pasar
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // =========================================================================
    // RS-03: trg_r6_22_creador (fn_r6_22_creador) - BEFORE INSERT OR UPDATE en menus
    // =========================================================================

    /// <summary>
    /// Prueba 17: nutricionista SIN fila en autoriza para la dieta; INSERT INTO menus → P0001 con fn_r6_22_creador.
    /// </summary>
    [Fact]
    public async Task Trg_r6_22_creador_rechaza_nutricionista_no_autorizado_en_insert()
    {
        var dietaId = await _sembrador.DietaAsync();
        var nutricionistaId = await _sembrador.NutricionistaAsync(); // NO autorizado

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico)
            VALUES (@id, @dieta_id, @nutricionista_creador_id, NOW() - INTERVAL '1 day', 1, FALSE)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("dieta_id", dietaId);
        cmd.Parameters.AddWithValue("nutricionista_creador_id", nutricionistaId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r6_22_creador", ex.Where);
    }

    /// <summary>
    /// Prueba 18: menú válido (nutricionista autorizado en dieta A, Sembrador.MenuAsync);
    /// UPDATE menus SET dieta_id = <dieta B donde no está autorizado> → P0001 con fn_r6_22_creador
    /// (es el primero de los tres triggers BEFORE UPDATE de menus por orden alfabético).
    /// Control (C2): con AutorizarAsync en la dieta B, el mismo UPDATE no lanza fn_r6_22_creador
    /// (puede fallar por otra regla de otro submodelo; basta con que la excepción, si ocurre, no sea de esa función).
    /// </summary>
    [Fact]
    public async Task Trg_r6_22_creador_rechaza_update_dieta_no_autorizada()
    {
        var dietaA = await _sembrador.DietaAsync();
        var dietaB = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaA); // autorizado solo en A

        var menuId = await _sembrador.MenuAsync(dietaA, creadorId);

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            UPDATE menus SET dieta_id = @dieta_b WHERE id = @menu_id
            """;
        cmd.Parameters.AddWithValue("dieta_b", dietaB);
        cmd.Parameters.AddWithValue("menu_id", menuId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r6_22_creador", ex.Where);
    }

    /// <summary>
    /// Control C2: autorizado en dieta B, el UPDATE no lanza fn_r6_22_creador.
    /// </summary>
    [Fact]
    public async Task Trg_r6_22_creador_acepta_update_si_autorizado_en_nueva_dieta()
    {
        var dietaA = await _sembrador.DietaAsync();
        var dietaB = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaA);
        await _sembrador.AutorizarAsync(creadorId, dietaB); // ahora también autorizado en B

        var menuId = await _sembrador.MenuAsync(dietaA, creadorId);

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                UPDATE menus SET dieta_id = @dieta_b WHERE id = @menu_id
                """;
            cmd.Parameters.AddWithValue("dieta_b", dietaB);
            cmd.Parameters.AddWithValue("menu_id", menuId);
            await cmd.ExecuteNonQueryAsync();

            // Si hay otra excepción, no debe ser de fn_r6_22_creador
            try
            {
                await tx.CommitAsync();
            }
            catch (PostgresException ex) when (ex.Where != null && ex.Where.Contains("fn_r6_22_creador"))
            {
                await tx.RollbackAsync();
                throw new Exception("No debió lanzar fn_r6_22_creador", ex);
            }
            catch
            {
                // Otra excepción (ej. RS-07, RS-15) es aceptable; hacemos rollback
                await tx.RollbackAsync();
            }
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // =========================================================================
    // RS-04: trg_r6_23 (fn_r6_23) - BEFORE INSERT OR UPDATE en asignacion
    // =========================================================================

    /// <summary>
    /// Prueba 19: menú válido; paciente NO inscrito en su dieta; INSERT INTO asignacion → P0001 con fn_r6_23.
    /// Control (C3): con InscribirAsync, el INSERT no lanza en la sentencia (el commit fallaría por RS-16
    /// porque el menú no está aprobado: es otro submodelo, hacemos Rollback sin confirmar).
    /// </summary>
    [Fact]
    public async Task Trg_r6_23_rechaza_paciente_no_inscrito_en_insert()
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);
        var menuId = await _sembrador.MenuAsync(dietaId, creadorId);
        var pacienteId = await _sembrador.PacienteAsync(); // NO inscrito

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO asignacion (menu_id, paciente_id)
            VALUES (@menu_id, @paciente_id)
            """;
        cmd.Parameters.AddWithValue("menu_id", menuId);
        cmd.Parameters.AddWithValue("paciente_id", pacienteId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r6_23", ex.Where);
    }

    /// <summary>
    /// Control C3: paciente inscrito, INSERT no lanza en la sentencia.
    /// </summary>
    [Fact]
    public async Task Trg_r6_23_acepta_insert_si_paciente_inscrito()
    {
        var dietaId = await _sembrador.DietaAsync();
        var creadorId = await _sembrador.NutricionistaAsync();
        await _sembrador.AutorizarAsync(creadorId, dietaId);
        var menuId = await _sembrador.MenuAsync(dietaId, creadorId);
        var pacienteId = await _sembrador.PacienteAsync();
        await _sembrador.InscribirAsync(pacienteId, dietaId);

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

            // El INSERT pasa; el commit fallaría por RS-16 (menú no aprobado), hacemos rollback
            await tx.RollbackAsync();
        }
        catch (PostgresException ex) when (ex.Where != null && ex.Where.Contains("fn_r6_23"))
        {
            await tx.RollbackAsync();
            throw new Exception("No debió lanzar fn_r6_23", ex);
        }
        catch
        {
            await tx.RollbackAsync();
            // Otra excepción (RS-16, etc.) es esperada
        }
    }

    // =========================================================================
    // RS-11: trg_r9_04 (fn_r9_04) - AFTER INSERT OR UPDATE en restringe/excluye/cubre_dieta, FOR EACH STATEMENT
    // =========================================================================

    /// <summary>
    /// Prueba 20: dieta con Requerido = Cereales; excluye ya contiene (Sin gluten, Cereales);
    /// INSERT INTO restringe (dieta_id, restriccion_alimentaria_id) con Sin gluten → P0001 con fn_r9_04.
    /// </summary>
    [Fact]
    public async Task Trg_r9_04_restringe_rechaza_si_grupo_requerido_excluido()
    {
        var grupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");
        var restriccionSinGluten = Guid.Parse("22222222-0000-0000-0000-000000000001");

        var dietaId = await _sembrador.DietaAsync(grupoCereales); // Requerido = Cereales

        // Sembrar excluye (Sin gluten, Cereales) - ya existe en InitialCreate pero verificamos/insertamos
        await using (var cmdExcluye = _conexion.CreateCommand())
        {
            cmdExcluye.CommandText = """
                INSERT INTO excluye (restriccion_alimentaria_id, grupo_nutricional_id)
                VALUES (@restriccion_id, @grupo_id)
                ON CONFLICT DO NOTHING
                """;
            cmdExcluye.Parameters.AddWithValue("restriccion_id", restriccionSinGluten);
            cmdExcluye.Parameters.AddWithValue("grupo_id", grupoCereales);
            await cmdExcluye.ExecuteNonQueryAsync();
        }

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO restringe (dieta_id, restriccion_alimentaria_id)
            VALUES (@dieta_id, @restriccion_id)
            """;
        cmd.Parameters.AddWithValue("dieta_id", dietaId);
        cmd.Parameters.AddWithValue("restriccion_id", restriccionSinGluten);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r9_04", ex.Where);
    }

    /// <summary>
    /// Prueba 21: dieta con Requerido = Cereales y restringe con Sin gluten ya insertados;
    /// INSERT INTO excluye (restriccion_alimentaria_id, grupo_nutricional_id) = (Sin gluten, Cereales) → P0001.
    /// </summary>
    [Fact]
    public async Task Trg_r9_04_excluye_rechaza_si_grupo_requerido_ya_restringido()
    {
        var grupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");
        var restriccionSinGluten = Guid.Parse("22222222-0000-0000-0000-000000000001");

        var dietaId = await _sembrador.DietaAsync(grupoCereales); // Requerido = Cereales

        // Insertar restringe (dieta, Sin gluten)
        await using (var cmdRestringe = _conexion.CreateCommand())
        {
            cmdRestringe.CommandText = """
                INSERT INTO restringe (dieta_id, restriccion_alimentaria_id)
                VALUES (@dieta_id, @restriccion_id)
                """;
            cmdRestringe.Parameters.AddWithValue("dieta_id", dietaId);
            cmdRestringe.Parameters.AddWithValue("restriccion_id", restriccionSinGluten);
            await cmdRestringe.ExecuteNonQueryAsync();
        }

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO excluye (restriccion_alimentaria_id, grupo_nutricional_id)
            VALUES (@restriccion_id, @grupo_id)
            """;
        cmd.Parameters.AddWithValue("restriccion_id", restriccionSinGluten);
        cmd.Parameters.AddWithValue("grupo_id", grupoCereales);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r9_04", ex.Where);
    }

    /// <summary>
    /// Prueba 22: restringe (Sin gluten) y excluye (Sin gluten, Cereales) ya existen para una dieta
    /// con un Requerido distinto (Lácteos); INSERT INTO cubre_dieta (Cereales, Requerido) → P0001.
    /// </summary>
    [Fact]
    public async Task Trg_r9_04_cubre_rechaza_si_grupo_excluido_se_vuelve_requerido()
    {
        var grupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");
        var grupoLacteos = Guid.Parse("11111111-0000-0000-0000-000000000003");
        var restriccionSinGluten = Guid.Parse("22222222-0000-0000-0000-000000000001");

        var dietaId = await _sembrador.DietaAsync(grupoLacteos); // Requerido = Lácteos

        // Insertar restringe (dieta, Sin gluten)
        await using (var cmdRestringe = _conexion.CreateCommand())
        {
            cmdRestringe.CommandText = """
                INSERT INTO restringe (dieta_id, restriccion_alimentaria_id)
                VALUES (@dieta_id, @restriccion_id)
                """;
            cmdRestringe.Parameters.AddWithValue("dieta_id", dietaId);
            cmdRestringe.Parameters.AddWithValue("restriccion_id", restriccionSinGluten);
            await cmdRestringe.ExecuteNonQueryAsync();
        }

        // Insertar excluye (Sin gluten, Cereales)
        await using (var cmdExcluye = _conexion.CreateCommand())
        {
            cmdExcluye.CommandText = """
                INSERT INTO excluye (restriccion_alimentaria_id, grupo_nutricional_id)
                VALUES (@restriccion_id, @grupo_id)
                """;
            cmdExcluye.Parameters.AddWithValue("restriccion_id", restriccionSinGluten);
            cmdExcluye.Parameters.AddWithValue("grupo_id", grupoCereales);
            await cmdExcluye.ExecuteNonQueryAsync();
        }

        // Ahora intentar añadir Cereales como Requerido
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo)
            VALUES (@dieta_id, @grupo_id, 'Requerido')
            """;
        cmd.Parameters.AddWithValue("dieta_id", dietaId);
        cmd.Parameters.AddWithValue("grupo_id", grupoCereales);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("P0001", ex.SqlState);
        Assert.Contains("fn_r9_04", ex.Where);
    }

    /// <summary>
    /// Control C4 (Prueba 23): igual que la 20 pero el grupo es Restringido (no Requerido): no lanza.
    /// El informe lo permite expresamente (línea 381).
    /// </summary>
    [Fact]
    public async Task Trg_r9_04_acepta_grupo_restringido_excluido_control_c4()
    {
        var grupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");
        var restriccionSinGluten = Guid.Parse("22222222-0000-0000-0000-000000000001");

        var dietaId = await _sembrador.DietaAsync(Guid.Parse("11111111-0000-0000-0000-000000000003")); // Requerido = Lácteos

        // Insertar excluye (Sin gluten, Cereales)
        await using (var cmdExcluye = _conexion.CreateCommand())
        {
            cmdExcluye.CommandText = """
                INSERT INTO excluye (restriccion_alimentaria_id, grupo_nutricional_id)
                VALUES (@restriccion_id, @grupo_id)
                """;
            cmdExcluye.Parameters.AddWithValue("restriccion_id", restriccionSinGluten);
            cmdExcluye.Parameters.AddWithValue("grupo_id", grupoCereales);
            await cmdExcluye.ExecuteNonQueryAsync();
        }

        // Insertar restringe (dieta, Sin gluten)
        await using (var cmdRestringe = _conexion.CreateCommand())
        {
            cmdRestringe.CommandText = """
                INSERT INTO restringe (dieta_id, restriccion_alimentaria_id)
                VALUES (@dieta_id, @restriccion_id)
                """;
            cmdRestringe.Parameters.AddWithValue("dieta_id", dietaId);
            cmdRestringe.Parameters.AddWithValue("restriccion_id", restriccionSinGluten);
            await cmdRestringe.ExecuteNonQueryAsync();
        }

        // Ahora añadir Cereales como Restringido (no Requerido) - debe pasar
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo)
            VALUES (@dieta_id, @grupo_id, 'Restringido')
            """;
        cmd.Parameters.AddWithValue("dieta_id", dietaId);
        cmd.Parameters.AddWithValue("grupo_id", grupoCereales);

        await cmd.ExecuteNonQueryAsync(); // No debe lanzar
    }
}
