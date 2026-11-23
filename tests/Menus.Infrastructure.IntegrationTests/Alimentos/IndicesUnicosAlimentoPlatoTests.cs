using System;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Alimentos;

/// <summary>
/// Pruebas de los índices únicos del submodelo Alimento-Plato (#200): nombres de los catálogos (RD-01, RD-02,
/// RD-03, RD-08) y nombre normalizado de los ítems (RC-01, RS-01). Los catálogos los siembra la migración
/// <c>InitialCreate</c>, y <see cref="PostgresFixture.ReiniciarAsync"/> no los borra.
/// </summary>
[Collection("PostgreSQL")]
public sealed class IndicesUnicosAlimentoPlatoTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _sembrador = null!;

    public IndicesUnicosAlimentoPlatoTests(PostgresFixture fixture)
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

    // 7. RD-03: no hay dos grupos nutricionales con el mismo nombre (InitialCreate.cs:734).
    [Fact]
    public async Task IX_grupos_nutricionales_nombre_rechaza_otro_grupo_Cereales()
    {
        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => InsertarEnCatalogoAsync("grupos_nutricionales", "Cereales"));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
        Assert.Equal("IX_grupos_nutricionales_nombre", ex.ConstraintName);
    }

    // 8. RD-01 y RD-08: no hay dos tipos de preparación con el mismo nombre (InitialCreate.cs:857).
    [Fact]
    public async Task IX_tipos_preparacion_nombre_rechaza_otro_tipo_Postre()
    {
        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => InsertarEnCatalogoAsync("tipos_preparacion", "Postre"));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
        Assert.Equal("IX_tipos_preparacion_nombre", ex.ConstraintName);
    }

    // 9. RD-02 y RD-08: no hay dos niveles calóricos con el mismo nombre (InitialCreate.cs:791).
    [Fact]
    public async Task IX_niveles_caloricos_nombre_rechaza_otro_nivel_Bajo()
    {
        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => InsertarEnCatalogoAsync("niveles_caloricos", "Bajo"));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
        Assert.Equal("IX_niveles_caloricos_nombre", ex.ConstraintName);
    }

    // 10(a). RC-01 y RS-01: el índice es sobre lower(btrim(nombre)), así que mayúsculas y espacios sobrantes no
    // distinguen un alimento de otro (Sql/R5_16_nombre_normalizado.sql).
    [Fact]
    public async Task Ux_items_alimentarios_r5_16_nombre_rechaza_el_mismo_alimento_con_otras_mayusculas_y_espacios()
    {
        await _sembrador.AlimentoAsync("Arroz");
        var nutricionistaId = await _sembrador.NutricionistaAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => _sembrador.ItemSinValidarAsync("  ARROZ ", "Alimento", nutricionistaId));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
        Assert.Equal("ux_items_alimentarios_r5_16_nombre", ex.ConstraintName);
    }

    // 10(b). Alimentos y platos comparten un solo espacio de nombres: un plato no se llama igual que un alimento.
    // El índice salta en el INSERT, antes que el trigger diferido RK-03 del plato sin composición.
    [Fact]
    public async Task Ux_items_alimentarios_r5_16_nombre_rechaza_un_plato_que_se_llama_como_un_alimento()
    {
        await _sembrador.AlimentoAsync("Sopa");
        var nutricionistaId = await _sembrador.NutricionistaAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => _sembrador.ItemSinValidarAsync("sopa", "Plato", nutricionistaId));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
        Assert.Equal("ux_items_alimentarios_r5_16_nombre", ex.ConstraintName);
    }

    // 10(c), control C3: el índice compara el nombre entero, no un prefijo.
    [Fact]
    public async Task Ux_items_alimentarios_r5_16_nombre_deja_convivir_Arroz_y_Arroz_integral()
    {
        await _sembrador.AlimentoAsync("Arroz");
        await _sembrador.AlimentoAsync("Arroz integral");

        Assert.Equal(2, await ContarItemsAsync("arroz%"));
    }

    /// <summary>Inserta una fila en una tabla de catálogo (<c>id</c>, <c>nombre</c>) y la confirma.</summary>
    /// <param name="tabla">Tabla del catálogo: una de las tres del submodelo.</param>
    /// <param name="nombre">Nombre de la fila.</param>
    private async Task InsertarEnCatalogoAsync(string tabla, string nombre)
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = tabla switch
        {
            "grupos_nutricionales" => "INSERT INTO grupos_nutricionales (id, nombre) VALUES (@id, @nombre)",
            "tipos_preparacion" => "INSERT INTO tipos_preparacion (id, nombre) VALUES (@id, @nombre)",
            "niveles_caloricos" => "INSERT INTO niveles_caloricos (id, nombre) VALUES (@id, @nombre)",
            _ => throw new ArgumentOutOfRangeException(nameof(tabla), tabla, "No es un catálogo del submodelo Alimento-Plato."),
        };
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("nombre", nombre);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<long> ContarItemsAsync(string patron)
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM items_alimentarios WHERE lower(nombre) LIKE @patron";
        cmd.Parameters.AddWithValue("patron", patron);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }
}
