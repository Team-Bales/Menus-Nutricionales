using System;
using System.Linq;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Migraciones;

/// <summary>
/// Pruebas de humo de la cadena de migraciones (Up completo, Down de un paso, Down a 0, Up de nuevo).
/// Cada prueba usa su propia base de datos vacía creada con <see cref="PostgresFixture.CrearBaseVaciaAsync"/>.
/// </summary>
[Collection("PostgreSQL")]
public sealed class CadenaDeMigracionesTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private string _cadenaBaseVacia = string.Empty;
    private MenusDbContext _ctx = null!;

    public CadenaDeMigracionesTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _cadenaBaseVacia = await _fixture.CrearBaseVaciaAsync();
        var opciones = new DbContextOptionsBuilder<MenusDbContext>()
            .UseNpgsql(_cadenaBaseVacia)
            .Options;
        _ctx = new MenusDbContext(opciones);
    }

    public async Task DisposeAsync()
    {
        await _ctx.DisposeAsync();
        // Limpia la base vacía creada para esta prueba
        await using var adminConn = new NpgsqlConnection(_fixture.CadenaDeConexion.Replace("Database=menus", "Database=postgres"));
        await adminConn.OpenAsync();
        var nombreBase = new NpgsqlConnectionStringBuilder(_cadenaBaseVacia).Database;
        await using var cmd = adminConn.CreateCommand();
        cmd.CommandText = $@"DROP DATABASE IF EXISTS ""{nombreBase}"" WITH (FORCE)";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 1. Up aplica todas las migraciones: 28 tablas de usuario, 26 triggers, 18 funciones fn_*,
    /// índice ux_items_alimentarios_r5_16_nombre.
    /// </summary>
    [Fact]
    public async Task Up_aplica_todas_las_migraciones()
    {
        // Act
        await _ctx.Database.MigrateAsync();

        // Assert - migraciones aplicadas
        var aplicadas = await _ctx.Database.GetAppliedMigrationsAsync();
        var todas = _ctx.Database.GetMigrations();
        Assert.Equal(todas.Count(), aplicadas.Count());

        // Assert - conteos en catálogo de PostgreSQL
        await using var conn = new NpgsqlConnection(_cadenaBaseVacia);
        await conn.OpenAsync();

        // 28 tablas de usuario (sin __EFMigrationsHistory)
        await using var cmdTablas = conn.CreateCommand();
        cmdTablas.CommandText = """
            SELECT count(*) FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            AND table_name != '__EFMigrationsHistory'
            """;
        var tablas = Convert.ToInt32(await cmdTablas.ExecuteScalarAsync());
        Assert.Equal(28, tablas);

        // 26 triggers de usuario (no internos)
        await using var cmdTriggers = conn.CreateCommand();
        cmdTriggers.CommandText = """
            SELECT count(*) FROM pg_trigger WHERE NOT tgisinternal
            """;
        var triggers = Convert.ToInt32(await cmdTriggers.ExecuteScalarAsync());
        Assert.Equal(26, triggers);

        // 18 funciones fn_*
        await using var cmdFunciones = conn.CreateCommand();
        cmdFunciones.CommandText = """
            SELECT count(*) FROM pg_proc WHERE proname LIKE 'fn\_%' ESCAPE '\'
            """;
        var funciones = Convert.ToInt32(await cmdFunciones.ExecuteScalarAsync());
        Assert.Equal(18, funciones);

        // índice ux_items_alimentarios_r5_16_nombre existe
        await using var cmdIndice = conn.CreateCommand();
        cmdIndice.CommandText = """
            SELECT 1 FROM pg_indexes WHERE indexname = 'ux_items_alimentarios_r5_16_nombre'
            """;
        var indiceExiste = await cmdIndice.ExecuteScalarAsync();
        Assert.NotNull(indiceExiste);
    }

    /// <summary>
    /// 2. Down de un paso quita es_jefe y su trigger: 25 triggers, 17 funciones.
    /// </summary>
    [Fact]
    public async Task Down_de_un_paso_quita_es_jefe_y_su_trigger()
    {
        // Arr: Up completo
        await _ctx.Database.MigrateAsync();

        // Act: Down a InitialCreate
        var migrator = _ctx.GetService<IMigrator>();
        await migrator.MigrateAsync("20261025234333_InitialCreate");

        // Assert
        await using var conn = new NpgsqlConnection(_cadenaBaseVacia);
        await conn.OpenAsync();

        // Columna es_jefe ya no existe
        await using var cmdColumna = conn.CreateCommand();
        cmdColumna.CommandText = """
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'nutricionistas' AND column_name = 'es_jefe'
            """;
        var columnaExiste = await cmdColumna.ExecuteScalarAsync();
        Assert.Null(columnaExiste);

        // fn_rs_03 ya no existe
        await using var cmdFn = conn.CreateCommand();
        cmdFn.CommandText = """
            SELECT 1 FROM pg_proc WHERE proname = 'fn_rs_03'
            """;
        var fnExiste = await cmdFn.ExecuteScalarAsync();
        Assert.Null(fnExiste);

        // 25 triggers
        await using var cmdTriggers = conn.CreateCommand();
        cmdTriggers.CommandText = """
            SELECT count(*) FROM pg_trigger WHERE NOT tgisinternal
            """;
        var triggers = Convert.ToInt32(await cmdTriggers.ExecuteScalarAsync());
        Assert.Equal(25, triggers);

        // 17 funciones fn_*
        await using var cmdFunciones = conn.CreateCommand();
        cmdFunciones.CommandText = """
            SELECT count(*) FROM pg_proc WHERE proname LIKE 'fn\_%' ESCAPE '\'
            """;
        var funciones = Convert.ToInt32(await cmdFunciones.ExecuteScalarAsync());
        Assert.Equal(17, funciones);
    }

    /// <summary>
    /// 3. Down a 0 deja la base sin objetos de usuario (solo __EFMigrationsHistory).
    /// </summary>
    [Fact]
    public async Task Down_a_cero_deja_la_base_sin_objetos()
    {
        // Arr: Up completo
        await _ctx.Database.MigrateAsync();

        // Act: Down a 0
        var migrator = _ctx.GetService<IMigrator>();
        await migrator.MigrateAsync("0");

        // Assert
        await using var conn = new NpgsqlConnection(_cadenaBaseVacia);
        await conn.OpenAsync();

        // Sin tablas de usuario
        await using var cmdTablas = conn.CreateCommand();
        cmdTablas.CommandText = """
            SELECT count(*) FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            AND table_name != '__EFMigrationsHistory'
            """;
        var tablas = Convert.ToInt32(await cmdTablas.ExecuteScalarAsync());
        Assert.Equal(0, tablas);

        // Sin triggers de usuario
        await using var cmdTriggers = conn.CreateCommand();
        cmdTriggers.CommandText = """
            SELECT count(*) FROM pg_trigger WHERE NOT tgisinternal
            """;
        var triggers = Convert.ToInt32(await cmdTriggers.ExecuteScalarAsync());
        Assert.Equal(0, triggers);

        // Sin funciones fn_*
        await using var cmdFunciones = conn.CreateCommand();
        cmdFunciones.CommandText = """
            SELECT count(*) FROM pg_proc WHERE proname LIKE 'fn\_%' ESCAPE '\'
            """;
        var funciones = Convert.ToInt32(await cmdFunciones.ExecuteScalarAsync());
        Assert.Equal(0, funciones);

        // Sin índice ux_items_alimentarios_r5_16_nombre
        await using var cmdIndice = conn.CreateCommand();
        cmdIndice.CommandText = """
            SELECT 1 FROM pg_indexes WHERE indexname = 'ux_items_alimentarios_r5_16_nombre'
            """;
        var indiceExiste = await cmdIndice.ExecuteScalarAsync();
        Assert.Null(indiceExiste);
    }

    /// <summary>
    /// 4. Up después de Down a 0 vuelve a funcionar (mismos conteos que prueba 1).
    /// </summary>
    [Fact]
    public async Task Up_despues_de_Down_a_cero_vuelve_a_funcionar()
    {
        // Arr: Up -> Down a 0
        await _ctx.Database.MigrateAsync();
        var migrator = _ctx.GetService<IMigrator>();
        await migrator.MigrateAsync("0");

        // Act: Up de nuevo
        await _ctx.Database.MigrateAsync();

        // Assert: mismos conteos que prueba 1
        await using var conn = new NpgsqlConnection(_cadenaBaseVacia);
        await conn.OpenAsync();

        await using var cmdTablas = conn.CreateCommand();
        cmdTablas.CommandText = """
            SELECT count(*) FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            AND table_name != '__EFMigrationsHistory'
            """;
        var tablas = Convert.ToInt32(await cmdTablas.ExecuteScalarAsync());
        Assert.Equal(28, tablas);

        await using var cmdTriggers = conn.CreateCommand();
        cmdTriggers.CommandText = """
            SELECT count(*) FROM pg_trigger WHERE NOT tgisinternal
            """;
        var triggers = Convert.ToInt32(await cmdTriggers.ExecuteScalarAsync());
        Assert.Equal(26, triggers);

        await using var cmdFunciones = conn.CreateCommand();
        cmdFunciones.CommandText = """
            SELECT count(*) FROM pg_proc WHERE proname LIKE 'fn\_%' ESCAPE '\'
            """;
        var funciones = Convert.ToInt32(await cmdFunciones.ExecuteScalarAsync());
        Assert.Equal(18, funciones);

        await using var cmdIndice = conn.CreateCommand();
        cmdIndice.CommandText = """
            SELECT 1 FROM pg_indexes WHERE indexname = 'ux_items_alimentarios_r5_16_nombre'
            """;
        var indiceExiste = await cmdIndice.ExecuteScalarAsync();
        Assert.NotNull(indiceExiste);
    }

    /// <summary>
    /// 5. ReiniciarAsync deja vacías las tablas de datos y conserva los catálogos.
    /// Demuestra que la limpieza no choca con los triggers de borrado.
    /// </summary>
    [Fact]
    public async Task ReiniciarAsync_deja_vacio_lo_sembrado_y_conserva_los_catalogos()
    {
        // Usa la base principal del fixture (ya migrada y con catálogos)
        await using var conn = await _fixture.AbrirAsync();
        var sembrador = new Sembrador(conn);

        // Crea un menú completo: Dieta -> Nutricionista (jefe) -> Autoriza -> Menu
        var dietaId = await sembrador.DietaAsync();
        var nutricionistaId = await sembrador.NutricionistaAsync(esJefe: true);
        await sembrador.AutorizarAsync(nutricionistaId, dietaId);
        await sembrador.MenuAsync(dietaId, nutricionistaId);

        // Verifica que hay datos antes de limpiar
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT count(*) FROM menus";
            Assert.Equal(1L, Convert.ToInt64(await cmd.ExecuteScalarAsync()));
        }

        // Act: ReiniciarAsync
        await _fixture.ReiniciarAsync();

        // Assert: tablas de datos vacías, catálogos con datos
        await using var cmdMenus = conn.CreateCommand();
        cmdMenus.CommandText = "SELECT count(*) FROM menus";
        Assert.Equal(0L, Convert.ToInt64(await cmdMenus.ExecuteScalarAsync()));

        await using var cmdMenuDist = conn.CreateCommand();
        cmdMenuDist.CommandText = "SELECT count(*) FROM menu_distribucion";
        Assert.Equal(0L, Convert.ToInt64(await cmdMenuDist.ExecuteScalarAsync()));

        await using var cmdNutris = conn.CreateCommand();
        cmdNutris.CommandText = "SELECT count(*) FROM nutricionistas";
        Assert.Equal(0L, Convert.ToInt64(await cmdNutris.ExecuteScalarAsync()));

        await using var cmdDietas = conn.CreateCommand();
        cmdDietas.CommandText = "SELECT count(*) FROM dietas";
        Assert.Equal(0L, Convert.ToInt64(await cmdDietas.ExecuteScalarAsync()));

        // Catálogos conservados
        await using var cmdEsp = conn.CreateCommand();
        cmdEsp.CommandText = "SELECT count(*) FROM especialidades";
        Assert.Equal(3L, Convert.ToInt64(await cmdEsp.ExecuteScalarAsync()));

        await using var cmdTipos = conn.CreateCommand();
        cmdTipos.CommandText = "SELECT count(*) FROM tipos_preparacion";
        Assert.Equal(4L, Convert.ToInt64(await cmdTipos.ExecuteScalarAsync()));

        await using var cmdResultados = conn.CreateCommand();
        cmdResultados.CommandText = "SELECT count(*) FROM resultados_nutricionales";
        Assert.Equal(5L, Convert.ToInt64(await cmdResultados.ExecuteScalarAsync()));
    }
}
