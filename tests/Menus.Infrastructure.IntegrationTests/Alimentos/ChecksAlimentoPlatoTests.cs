using System;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Alimentos;

/// <summary>
/// Pruebas de los CHECK del submodelo Alimento-Plato (#200): RD-01, RD-02, RD-04, RD-05 y RD-17, contra un
/// PostgreSQL 16 creado con las migraciones. Cada prueba comprueba el código de error y el nombre del constraint,
/// nunca el texto del mensaje.
/// </summary>
[Collection("PostgreSQL")]
public sealed class ChecksAlimentoPlatoTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _sembrador = null!;

    public ChecksAlimentoPlatoTests(PostgresFixture fixture)
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

    // 1. RD-04: el nombre del grupo nutricional no puede estar en blanco (InitialCreate.cs:40).
    [Fact]
    public async Task Ck_grupos_nutricionales_r5_11_nombre_rechaza_un_nombre_en_blanco()
    {
        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => _sembrador.CatalogoSinValidarAsync("grupos_nutricionales", "   "));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Equal("ck_grupos_nutricionales_r5_11_nombre", ex.ConstraintName);
    }

    // 2. RD-01: el tipo de preparación es uno de los cuatro del catálogo cerrado (InitialCreate.cs:120).
    [Fact]
    public async Task Ck_tipos_preparacion_r5_08_nombre_rechaza_un_tipo_fuera_del_catalogo()
    {
        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => _sembrador.CatalogoSinValidarAsync("tipos_preparacion", "Aperitivo"));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Equal("ck_tipos_preparacion_r5_08_nombre", ex.ConstraintName);
    }

    // 3. RD-02: el nivel calórico es Bajo, Medio o Alto (InitialCreate.cs:53).
    [Fact]
    public async Task Ck_niveles_caloricos_r5_09_nombre_rechaza_un_nivel_fuera_del_catalogo()
    {
        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => _sembrador.CatalogoSinValidarAsync("niveles_caloricos", "Extremo"));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Equal("ck_niveles_caloricos_r5_09_nombre", ex.ConstraintName);
    }

    // 4. RD-04: el nombre del ítem no puede estar en blanco (InitialCreate.cs:224).
    [Fact]
    public async Task Ck_items_alimentarios_r5_11_nombre_rechaza_un_nombre_en_blanco()
    {
        var nutricionistaId = await _sembrador.NutricionistaAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => _sembrador.ItemSinValidarAsync(" ", "Alimento", nutricionistaId));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Equal("ck_items_alimentarios_r5_11_nombre", ex.ConstraintName);
    }

    // 5. RD-17: la clase del ítem es Alimento o Plato (InitialCreate.cs:225).
    [Fact]
    public async Task Ck_items_alimentarios_r5_19_clase_rechaza_una_clase_desconocida()
    {
        var nutricionistaId = await _sembrador.NutricionistaAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => _sembrador.ItemSinValidarAsync("Jugo de naranja", "Bebida", nutricionistaId));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Equal("ck_items_alimentarios_r5_19_clase", ex.ConstraintName);
    }

    // 6. RD-05: la cantidad de un componente es mayor que cero (InitialCreate.cs:361).
    [Fact]
    public async Task Ck_composicion_r5_12_cantidad_rechaza_una_cantidad_cero()
    {
        var primero = await _sembrador.AlimentoAsync();
        var platoId = await _sembrador.PlatoAsync(primero);
        var segundo = await _sembrador.AlimentoAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => _sembrador.ComposicionSinValidarAsync(platoId, segundo, cantidad: 0));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Equal("ck_composicion_r5_12_cantidad", ex.ConstraintName);
    }
}
