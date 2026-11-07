using System;
using System.Data;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Dietas;

/// <summary>
/// Pruebas de los índices únicos del submodelo Dieta-Nutricionista-Paciente-Sala (RD-08).
/// Cada prueba verifica que el índice único rechaza el duplicado.
/// </summary>
[Collection("PostgreSQL")]
public sealed class IndicesUnicosDietasTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _sembrador = null!;

    public IndicesUnicosDietasTests(PostgresFixture fixture)
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

    // RD-08: Nombre único en nomencladores
    // IX_programas_atencion_nombre (InitialCreate.cs:812) - "Oncología" ya sembrado
    [Fact]
    public async Task IX_programas_atencion_nombre_rechaza_duplicado()
    {
        // "Oncología" tiene ID 33333333-0000-0000-0000-000000000002 y ya existe
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO programas_atencion (id, nombre)
            VALUES (@id, 'Oncología')
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23505", ex.SqlState);
        Assert.Equal("IX_programas_atencion_nombre", ex.ConstraintName);
    }

    [Fact]
    public async Task IX_programas_atencion_nombre_acepta_nombre_nuevo()
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO programas_atencion (id, nombre)
                VALUES (@id, 'Nuevo Programa')
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

    // IX_restricciones_alimentarias_nombre (InitialCreate.cs:818) - "Vegana" ya sembrada
    [Fact]
    public async Task IX_restricciones_alimentarias_nombre_rechaza_duplicado()
    {
        // "Vegana" tiene ID 22222222-0000-0000-0000-000000000005 y ya existe
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO restricciones_alimentarias (id, nombre)
            VALUES (@id, 'Vegana')
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23505", ex.SqlState);
        Assert.Equal("IX_restricciones_alimentarias_nombre", ex.ConstraintName);
    }

    [Fact]
    public async Task IX_restricciones_alimentarias_nombre_acepta_nombre_nuevo()
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO restricciones_alimentarias (id, nombre)
                VALUES (@id, 'Nueva Restricción')
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

    // IX_especialidades_nombre (InitialCreate.cs:723) - "Nutrición Deportiva" ya sembrada
    [Fact]
    public async Task IX_especialidades_nombre_rechaza_duplicado()
    {
        // "Nutrición Deportiva" tiene ID 44444444-0000-0000-0000-000000000003 y ya existe
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO especialidades (id, nombre)
            VALUES (@id, 'Nutrición Deportiva')
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23505", ex.SqlState);
        Assert.Equal("IX_especialidades_nombre", ex.ConstraintName);
    }

    [Fact]
    public async Task IX_especialidades_nombre_acepta_nombre_nuevo()
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO especialidades (id, nombre)
                VALUES (@id, 'Nueva Especialidad')
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

    // Control del informe §4.5 (línea 461): dos dietas, dos nutricionistas, dos pacientes y dos salas
    // con el mismo nombre conviven (no hay índice único; es deliberado)
    // Este control ya se prueba en ChecksDietasTests.Nombres_repetidos_permitidos_en_dieta_nutricionista_paciente_sala
    // Aquí solo verificamos que los índices únicos SÍ existen en los nomencladores
    [Fact]
    public async Task Indices_unicos_existen_en_nomencladores_y_no_en_entidades_principales()
    {
        // Verifica que los índices únicos existen en los catálogos
        var indices = new System.Collections.Generic.List<string>();
        {
            await using var cmdIdx = _conexion.CreateCommand();
            cmdIdx.CommandText = """
                SELECT indexname FROM pg_indexes
                WHERE indexname IN ('IX_programas_atencion_nombre', 'IX_restricciones_alimentarias_nombre', 'IX_especialidades_nombre')
                AND schemaname = 'public'
                """;
            await using var reader = await cmdIdx.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                indices.Add(reader.GetString(0));
            }
        } // reader y cmdIdx se disponen aquí

        Assert.Contains("IX_programas_atencion_nombre", indices);
        Assert.Contains("IX_restricciones_alimentarias_nombre", indices);
        Assert.Contains("IX_especialidades_nombre", indices);
        Assert.Equal(3, indices.Count);

        // Verifica que NO hay índices únicos en nombre de dietas, nutricionistas, pacientes, salas
        var indicesUnicosEntidades = new System.Collections.Generic.List<string>();
        {
            await using var cmdNoIdx = _conexion.CreateCommand();
            cmdNoIdx.CommandText = """
                SELECT indexname FROM pg_indexes
                WHERE tablename IN ('dietas', 'nutricionistas', 'pacientes', 'salas')
                AND schemaname = 'public'
                AND indexdef LIKE '%UNIQUE%'
                AND indexdef LIKE '%nombre%'
                """;
            await using var reader2 = await cmdNoIdx.ExecuteReaderAsync();
            while (await reader2.ReadAsync())
            {
                indicesUnicosEntidades.Add(reader2.GetString(0));
            }
        } // reader2 y cmdNoIdx se disponen aquí

        Assert.Empty(indicesUnicosEntidades);
    }
}
