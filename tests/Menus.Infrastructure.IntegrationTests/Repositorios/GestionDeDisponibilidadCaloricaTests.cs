using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Pruebas contra PostgreSQL real de las tres consultas que arman la disponibilidad calórica de una dieta
/// (informe, «R1»; #101): los grupos prohibidos, el conteo del banco y el conteo de los menús finales.
/// </summary>
[Collection("PostgreSQL")]
public sealed class GestionDeDisponibilidadCaloricaTests : IAsyncLifetime
{
    private static readonly Guid SinGluten = Guid.Parse("22222222-0000-0000-0000-000000000001");

    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _s = null!;

    public GestionDeDisponibilidadCaloricaTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();
        _conexion = await _fixture.AbrirAsync();
        _s = new Sembrador(_conexion);
    }

    public async Task DisposeAsync() => await _conexion.DisposeAsync();

    /// <summary>Marca un grupo como <c>Restringido</c> en la cobertura de la dieta.</summary>
    private async Task RestringirGrupoAsync(Guid dietaId, Guid grupoId)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var cmd = _conexion.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo) VALUES (@d, @g, 'Restringido')";
        cmd.Parameters.AddWithValue("d", dietaId);
        cmd.Parameters.AddWithValue("g", grupoId);
        await cmd.ExecuteNonQueryAsync();
        await tx.CommitAsync();
    }

    /// <summary>Aplica una restricción alimentaria ya sembrada (p. ej. <see cref="SinGluten"/>) a la dieta.</summary>
    private async Task AplicarRestriccionAsync(Guid dietaId, Guid restriccionId)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var cmd = _conexion.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "INSERT INTO restringe (dieta_id, restriccion_alimentaria_id) VALUES (@d, @r)";
        cmd.Parameters.AddWithValue("d", dietaId);
        cmd.Parameters.AddWithValue("r", restriccionId);
        await cmd.ExecuteNonQueryAsync();
        await tx.CommitAsync();
    }

    /// <summary>Registra que una restricción alimentaria ya sembrada excluye un grupo.</summary>
    private async Task ExcluirGrupoAsync(Guid restriccionId, Guid grupoId)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var cmd = _conexion.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "INSERT INTO excluye (restriccion_alimentaria_id, grupo_nutricional_id) VALUES (@r, @g)";
        cmd.Parameters.AddWithValue("r", restriccionId);
        cmd.Parameters.AddWithValue("g", grupoId);
        await cmd.ExecuteNonQueryAsync();
        await tx.CommitAsync();
    }

    [Fact]
    public async Task GetGruposProhibidosAsync_combina_restringidos_y_excluidos_por_restriccion()
    {
        var grupoRequerido = Sembrador.GrupoCerealesId;
        var grupoRestringido = Sembrador.GrupoProteinas;
        var dietaId = await _s.DietaAsync(grupoRequerido);
        await RestringirGrupoAsync(dietaId, grupoRestringido);

        var otroGrupoExcluido = Guid.Parse("11111111-0000-0000-0000-000000000004"); // Frutas, seed de InitialCreate
        await AplicarRestriccionAsync(dietaId, SinGluten);
        await ExcluirGrupoAsync(SinGluten, otroGrupoExcluido);

        await using var ctx = _fixture.CrearContexto();
        var prohibidos = await new DietaRepository(ctx).GetGruposProhibidosAsync(dietaId);

        Assert.Contains(grupoRestringido, prohibidos);
        Assert.Contains(otroGrupoExcluido, prohibidos);
        Assert.DoesNotContain(grupoRequerido, prohibidos);
    }

    [Fact]
    public async Task ContarPorNivelCaloricoAsync_excluye_los_grupos_prohibidos()
    {
        var grupoPermitido = Sembrador.GrupoCerealesId;
        var grupoProhibido = Sembrador.GrupoProteinas;

        await _s.ItemAsync("Alimento", grupoPermitido, nivelId: NivelCalorico.Bajo.Id);
        await _s.ItemAsync("Alimento", grupoPermitido, nivelId: NivelCalorico.Bajo.Id);
        await _s.ItemAsync("Alimento", grupoProhibido, nivelId: NivelCalorico.Bajo.Id);
        await _s.ItemAsync("Alimento", grupoPermitido, nivelId: NivelCalorico.Alto.Id);

        await using var ctx = _fixture.CrearContexto();
        var conteo = await new ItemAlimentarioRepository(ctx).ContarPorNivelCaloricoAsync([grupoProhibido]);

        Assert.Equal(2, conteo[NivelCalorico.Bajo.Id]);
        Assert.Equal(1, conteo[NivelCalorico.Alto.Id]);
        Assert.False(conteo.ContainsKey(NivelCalorico.Medio.Id));
    }

    [Fact]
    public async Task ContarItemsFinalesPorNivelCaloricoAsync_solo_cuenta_items_de_menus_aprobados_de_la_dieta()
    {
        var dietaId = await _s.DietaAsync();
        var creadorId = await _s.NutricionistaAsync();
        await _s.AutorizarAsync(creadorId, dietaId);
        var revisorId = await _s.NutricionistaAsync(esJefe: true);
        await _s.AutorizarAsync(revisorId, dietaId);

        var itemBajo = await _s.ItemAsync("Alimento", nivelId: NivelCalorico.Bajo.Id);
        var itemAlto = await _s.ItemAsync("Alimento", nivelId: NivelCalorico.Alto.Id);

        var menuFinal = await _s.MenuAsync(dietaId, creadorId);
        await _s.IncluirEnMenuAsync(menuFinal, itemBajo, itemAlto);
        await _s.ValidarAsync(menuFinal, revisorId, aprobado: true);

        var menuPendiente = await _s.MenuAsync(dietaId, creadorId);
        await _s.IncluirEnMenuAsync(menuPendiente, itemBajo);

        await using var ctx = _fixture.CrearContexto();
        var conteo = await new MenuRepository(ctx).ContarItemsFinalesPorNivelCaloricoAsync(dietaId);

        Assert.Equal(1, conteo[NivelCalorico.Bajo.Id]);
        Assert.Equal(1, conteo[NivelCalorico.Alto.Id]);
    }
}
