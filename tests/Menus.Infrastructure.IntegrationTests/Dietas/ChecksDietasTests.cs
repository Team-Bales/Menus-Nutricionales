using System;
using System.Data;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Dietas;

/// <summary>
/// Pruebas de los CHECK del submodelo Dieta-Nutricionista-Paciente-Sala (RD-06, RD-07, RD-16).
/// Cada prueba verifica que el constraint rechaza el valor inválido y deja pasar el válido.
/// </summary>
[Collection("PostgreSQL")]
public sealed class ChecksDietasTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _sembrador = null!;

    public ChecksDietasTests(PostgresFixture fixture)
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

    // RD-07: Nombre obligatorio y no vacío (btrim(nombre) <> '') en 7 tablas
    // ck_programas_atencion_r6_15_nombre (InitialCreate.cs:66)
    [Fact]
    public async Task Ck_programas_atencion_r6_15_nombre_rechaza_nombre_vacio()
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO programas_atencion (id, nombre)
            VALUES (@id, ' ')
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_programas_atencion_r6_15_nombre", ex.ConstraintName);
    }

    [Fact]
    public async Task Ck_programas_atencion_r6_15_nombre_acepta_nombre_valido()
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO programas_atencion (id, nombre)
                VALUES (@id, 'Programa Válido')
                """;
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());

            await cmd.ExecuteNonQueryAsync();
            await tx.RollbackAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ck_restricciones_alimentarias_r6_15_nombre (InitialCreate.cs:79)
    [Fact]
    public async Task Ck_restricciones_alimentarias_r6_15_nombre_rechaza_nombre_vacio()
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO restricciones_alimentarias (id, nombre)
            VALUES (@id, ' ')
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_restricciones_alimentarias_r6_15_nombre", ex.ConstraintName);
    }

    [Fact]
    public async Task Ck_restricciones_alimentarias_r6_15_nombre_acepta_nombre_valido()
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO restricciones_alimentarias (id, nombre)
                VALUES (@id, 'Restricción Válida')
                """;
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());

            await cmd.ExecuteNonQueryAsync();
            await tx.RollbackAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ck_especialidades_r6_15_nombre (InitialCreate.cs:27)
    [Fact]
    public async Task Ck_especialidades_r6_15_nombre_rechaza_nombre_vacio()
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO especialidades (id, nombre)
            VALUES (@id, ' ')
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_especialidades_r6_15_nombre", ex.ConstraintName);
    }

    [Fact]
    public async Task Ck_especialidades_r6_15_nombre_acepta_nombre_valido()
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO especialidades (id, nombre)
                VALUES (@id, 'Especialidad Válida')
                """;
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());

            await cmd.ExecuteNonQueryAsync();
            await tx.RollbackAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ck_salas_r6_15_nombre (InitialCreate.cs:107)
    [Fact]
    public async Task Ck_salas_r6_15_nombre_rechaza_nombre_vacio()
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO salas (id, nombre)
            VALUES (@id, ' ')
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_salas_r6_15_nombre", ex.ConstraintName);
    }

    [Fact]
    public async Task Ck_salas_r6_15_nombre_acepta_nombre_valido()
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO salas (id, nombre)
            VALUES (@id, 'Sala Válida')
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());

        await cmd.ExecuteNonQueryAsync();
    }

    // ck_nutricionistas_r6_15_nombre (InitialCreate.cs:134)
    [Fact]
    public async Task Ck_nutricionistas_r6_15_nombre_rechaza_nombre_vacio()
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO nutricionistas (id, nombre, especialidad_id)
            VALUES (@id, ' ', @especialidad_id)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("especialidad_id", Guid.Parse("44444444-0000-0000-0000-000000000001"));

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_nutricionistas_r6_15_nombre", ex.ConstraintName);
    }

    [Fact]
    public async Task Ck_nutricionistas_r6_15_nombre_acepta_nombre_valido()
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO nutricionistas (id, nombre, especialidad_id)
            VALUES (@id, 'Nutricionista Válido', @especialidad_id)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("especialidad_id", Guid.Parse("44444444-0000-0000-0000-000000000001"));

        await cmd.ExecuteNonQueryAsync();
    }

    // ck_dietas_r6_15_nombre (InitialCreate.cs:154)
    [Fact]
    public async Task Ck_dietas_r6_15_nombre_rechaza_nombre_vacio()
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO dietas (id, nombre, programa_atencion_id)
            VALUES (@id, ' ', @programa_atencion_id)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("programa_atencion_id", Guid.Parse("33333333-0000-0000-0000-000000000001"));

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_dietas_r6_15_nombre", ex.ConstraintName);
    }

    [Fact]
    public async Task Ck_dietas_r6_15_nombre_acepta_nombre_valido()
    {
        var dietaId = Guid.NewGuid();
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO dietas (id, nombre, programa_atencion_id)
                VALUES (@id, @nombre, @programa_atencion_id)
                """;
            cmd.Parameters.AddWithValue("id", dietaId);
            cmd.Parameters.AddWithValue("nombre", "Dieta Válida");
            cmd.Parameters.AddWithValue("programa_atencion_id", Guid.Parse("33333333-0000-0000-0000-000000000001"));
            await cmd.ExecuteNonQueryAsync();

            // RK-07: toda dieta debe tener al menos una cobertura Requerido (trg_r6_21_dieta diferido)
            cmd.CommandText = """
                INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo)
                VALUES (@dieta_id, @grupo_nutricional_id, 'Requerido')
                """;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            cmd.Parameters.AddWithValue("grupo_nutricional_id", Guid.Parse("11111111-0000-0000-0000-000000000001")); // Cereales
            await cmd.ExecuteNonQueryAsync();

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ck_pacientes_r6_15_nombre (InitialCreate.cs:200)
    [Fact]
    public async Task Ck_pacientes_r6_15_nombre_rechaza_nombre_vacio()
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO pacientes (id, nombre, edad, sala_id)
            VALUES (@id, ' ', 30, @sala_id)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("sala_id", await _sembrador.SalaAsync());

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_pacientes_r6_15_nombre", ex.ConstraintName);
    }

    [Fact]
    public async Task Ck_pacientes_r6_15_nombre_acepta_nombre_valido()
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO pacientes (id, nombre, edad, sala_id)
            VALUES (@id, 'Paciente Válido', 30, @sala_id)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("sala_id", await _sembrador.SalaAsync());

        await cmd.ExecuteNonQueryAsync();
    }

    // RD-06: Edad >= 0 (ck_pacientes_r6_14_edad, InitialCreate.cs:199)
    [Fact]
    public async Task Ck_pacientes_r6_14_edad_rechaza_edad_negativa()
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO pacientes (id, nombre, edad, sala_id)
            VALUES (@id, 'Paciente Test', -1, @sala_id)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("sala_id", await _sembrador.SalaAsync());

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_pacientes_r6_14_edad", ex.ConstraintName);
    }

    [Fact]
    public async Task Ck_pacientes_r6_14_edad_acepta_edad_cero()
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO pacientes (id, nombre, edad, sala_id)
            VALUES (@id, 'Paciente Test', 0, @sala_id)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("sala_id", await _sembrador.SalaAsync());

        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Ck_pacientes_r6_14_edad_acepta_edad_positiva()
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO pacientes (id, nombre, edad, sala_id)
            VALUES (@id, 'Paciente Test', 25, @sala_id)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("sala_id", await _sembrador.SalaAsync());

        await cmd.ExecuteNonQueryAsync();
    }

    // RD-16: Tipo de cubre_dieta IN ('Requerido', 'Restringido') (ck_cubre_dieta_r6_17b_tipo, InitialCreate.cs:287)
    [Fact]
    public async Task Ck_cubre_dieta_r6_17b_tipo_rechaza_tipo_invalido()
    {
        var dietaId = await _sembrador.DietaAsync();

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo)
            VALUES (@dieta_id, @grupo_id, 'Opcional')
            """;
        cmd.Parameters.AddWithValue("dieta_id", dietaId);
        cmd.Parameters.AddWithValue("grupo_id", Guid.Parse("11111111-0000-0000-0000-000000000002")); // Proteínas

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_cubre_dieta_r6_17b_tipo", ex.ConstraintName);
    }

    [Fact]
    public async Task Ck_cubre_dieta_r6_17b_tipo_acepta_requerido()
    {
        var dietaId = await _sembrador.DietaAsync();

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo)
            VALUES (@dieta_id, @grupo_id, 'Requerido')
            """;
        cmd.Parameters.AddWithValue("dieta_id", dietaId);
        cmd.Parameters.AddWithValue("grupo_id", Guid.Parse("11111111-0000-0000-0000-000000000002")); // Proteínas

        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Ck_cubre_dieta_r6_17b_tipo_acepta_restringido()
    {
        var dietaId = await _sembrador.DietaAsync();

        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo)
            VALUES (@dieta_id, @grupo_id, 'Restringido')
            """;
        cmd.Parameters.AddWithValue("dieta_id", dietaId);
        cmd.Parameters.AddWithValue("grupo_id", Guid.Parse("11111111-0000-0000-0000-000000000002")); // Proteínas

        await cmd.ExecuteNonQueryAsync();
    }

    // Control: nombres repetidos permitidos en Dieta, Nutricionista, Paciente, Sala (informe §4.5 línea 461)
    [Fact]
    public async Task Nombres_repetidos_permitidos_en_dieta_nutricionista_paciente_sala()
    {
        var salaId = await _sembrador.SalaAsync();
        var nombre = "Nombre Repetido";

        // Dos salas con el mismo nombre
        await using (var cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO salas (id, nombre) VALUES (@id, @nombre)";
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("nombre", nombre);
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO salas (id, nombre) VALUES (@id, @nombre)";
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("nombre", nombre);
            await cmd.ExecuteNonQueryAsync();
        }

        // Dos nutricionistas con el mismo nombre
        await using (var cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO nutricionistas (id, nombre, especialidad_id) VALUES (@id, @nombre, @esp)";
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("nombre", nombre);
            cmd.Parameters.AddWithValue("esp", Guid.Parse("44444444-0000-0000-0000-000000000001"));
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO nutricionistas (id, nombre, especialidad_id) VALUES (@id, @nombre, @esp)";
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("nombre", nombre);
            cmd.Parameters.AddWithValue("esp", Guid.Parse("44444444-0000-0000-0000-000000000001"));
            await cmd.ExecuteNonQueryAsync();
        }

        // Dos pacientes con el mismo nombre
        await using (var cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO pacientes (id, nombre, edad, sala_id) VALUES (@id, @nombre, 30, @sala)";
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("nombre", nombre);
            cmd.Parameters.AddWithValue("sala", salaId);
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO pacientes (id, nombre, edad, sala_id) VALUES (@id, @nombre, 30, @sala)";
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("nombre", nombre);
            cmd.Parameters.AddWithValue("sala", salaId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Dos dietas con el mismo nombre (cada una con su cobertura Requerido para RK-07)
        var grupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");
        var programaPediatria = Guid.Parse("33333333-0000-0000-0000-000000000001");

        await using (var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            try
            {
                await using (var cmd = _conexion.CreateCommand())
                {
                    cmd.Transaction = tx;
                    var dietaId1 = Guid.NewGuid();
                    cmd.CommandText = "INSERT INTO dietas (id, nombre, programa_atencion_id) VALUES (@id, @nombre, @prog)";
                    cmd.Parameters.AddWithValue("id", dietaId1);
                    cmd.Parameters.AddWithValue("nombre", nombre);
                    cmd.Parameters.AddWithValue("prog", programaPediatria);
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = "INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo) VALUES (@dieta_id, @grupo_id, 'Requerido')";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("dieta_id", dietaId1);
                    cmd.Parameters.AddWithValue("grupo_id", grupoCereales);
                    await cmd.ExecuteNonQueryAsync();
                }

                await using (var cmd = _conexion.CreateCommand())
                {
                    cmd.Transaction = tx;
                    var dietaId2 = Guid.NewGuid();
                    cmd.CommandText = "INSERT INTO dietas (id, nombre, programa_atencion_id) VALUES (@id, @nombre, @prog)";
                    cmd.Parameters.AddWithValue("id", dietaId2);
                    cmd.Parameters.AddWithValue("nombre", nombre);
                    cmd.Parameters.AddWithValue("prog", programaPediatria);
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = "INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo) VALUES (@dieta_id, @grupo_id, 'Requerido')";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("dieta_id", dietaId2);
                    cmd.Parameters.AddWithValue("grupo_id", grupoCereales);
                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
