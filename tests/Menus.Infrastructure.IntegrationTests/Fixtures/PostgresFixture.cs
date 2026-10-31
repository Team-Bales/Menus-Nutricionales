using System;
using System.Data.Common;
using System.Threading.Tasks;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Fixture de xUnit que levanta un contenedor PostgreSQL 16 con Testcontainers,
/// aplica las migraciones de EF Core y expone utilidades para limpiar la base entre pruebas.
/// </summary>
/// <remarks>
/// Implementa <see cref="IAsyncLifetime"/> para que xUnit gestione el ciclo de vida:
/// <list type="number">
///   <item><see cref="InitializeAsync"/> arranca el contenedor, crea la base <c>menus</c>,
///   ejecuta <c>Database.MigrateAsync()</c> y prepara <see cref="Checkpoint"/>.</item>
///   <item>Las pruebas usan <see cref="CrearContexto"/>, <see cref="AbrirAsync"/>,
///   <see cref="ReiniciarAsync"/> y <see cref="CrearBaseVaciaAsync"/>.</item>
///   <item><see cref="DisposeAsync"/> detiene y elimina el contenedor.</item>
/// </list>
/// El contenedor se comparte por colección (ver <see cref="PostgresCollection"/>).
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _contenedor;
    private Checkpoint _checkpoint = null!;
    private DbContextOptions<MenusDbContext> _opcionesContexto = null!;

    /// <summary>
    /// Inicializa el fixture con el contenedor PostgreSQL 16.
    /// </summary>
    public PostgresFixture()
    {
        _contenedor = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .Build();
    }

    /// <summary>
    /// Cadena de conexión a la base <c>menus</c> del contenedor.
    /// </summary>
    public string CadenaDeConexion { get; private set; } = string.Empty;

    /// <summary>
    /// Arranca el contenedor, crea la base <c>menus</c>, aplica migraciones y prepara Respawn.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();

        var adminConnectionString = _contenedor.GetConnectionString();
        var builder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = "menus"
        };
        CadenaDeConexion = builder.ToString();

        // Crea la base "menus" si no existe (el contenedor trae "postgres" por defecto)
        await using (var adminConn = new NpgsqlConnection(adminConnectionString))
        {
            await adminConn.OpenAsync();
            await using var cmd = adminConn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'menus'";
            var exists = await cmd.ExecuteScalarAsync();
            if (exists == null)
            {
                cmd.CommandText = "CREATE DATABASE menus";
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // Configura DbContext y ejecuta migraciones
        _opcionesContexto = new DbContextOptionsBuilder<MenusDbContext>()
            .UseNpgsql(CadenaDeConexion)
            .Options;

        await using var ctx = new MenusDbContext(_opcionesContexto);
        await ctx.Database.MigrateAsync();

        // Prepara Respawn (Checkpoint) ignorando la tabla de historial de migraciones y los 7 catálogos sembrados
        _checkpoint = new Checkpoint
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore =
            [
                new Table("__EFMigrationsHistory"),
                new Table("especialidades"),
                new Table("grupos_nutricionales"),
                new Table("niveles_caloricos"),
                new Table("programas_atencion"),
                new Table("restricciones_alimentarias"),
                new Table("resultados_nutricionales"),
                new Table("tipos_preparacion")
            ],
            WithReseed = false
        };
    }

    /// <summary>
    /// Crea un nuevo <see cref="MenusDbContext"/> conectado a la base migrada.
    /// </summary>
    public MenusDbContext CrearContexto() => new(_opcionesContexto);

    /// <summary>
    /// Abre una nueva conexión Npgsql a la base <c>menus</c>.
    /// </summary>
    public async Task<NpgsqlConnection> AbrirAsync()
    {
        var conn = new NpgsqlConnection(CadenaDeConexion);
        await conn.OpenAsync();
        return conn;
    }

    /// <summary>
    /// Limpia todas las tablas de datos (menos catálogos e historial) usando Respawn.
    /// Ejecuta <c>SET session_replication_role = replica;</c> para evitar disparar
    /// los triggers de borrado restrictivos.
    /// </summary>
    public async Task ReiniciarAsync()
    {
        await using var conn = new NpgsqlConnection(CadenaDeConexion);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SET session_replication_role = replica;";
        await cmd.ExecuteNonQueryAsync();
        await _checkpoint.Reset(conn);
    }

    /// <summary>
    /// Crea una base de datos vacía (sin migraciones) con nombre único y devuelve su cadena de conexión.
    /// La usan las pruebas de la cadena de migraciones (<c>Down a 0</c> destruye la base).
    /// </summary>
    public async Task<string> CrearBaseVaciaAsync()
    {
        var nombreBase = $"menus_{Guid.NewGuid():N}";
        var adminConnectionString = _contenedor.GetConnectionString();

        await using var adminConn = new NpgsqlConnection(adminConnectionString);
        await adminConn.OpenAsync();
        await using var cmd = adminConn.CreateCommand();
        cmd.CommandText = $@"CREATE DATABASE ""{nombreBase}""";
        await cmd.ExecuteNonQueryAsync();

        var builder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = nombreBase
        };
        return builder.ToString();
    }

    /// <summary>
    /// Detiene y elimina el contenedor Testcontainers.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _contenedor.DisposeAsync();
    }
}
