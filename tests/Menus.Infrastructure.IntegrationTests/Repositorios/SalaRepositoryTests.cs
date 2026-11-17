using System;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>Pruebas contra PostgreSQL real de <see cref="SalaRepository.ExisteNombreAsync"/> (#72).</summary>
[Collection("PostgreSQL")]
public sealed class SalaRepositoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;

    public SalaRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();
        _conexion = await _fixture.AbrirAsync();
    }

    public async Task DisposeAsync() => await _conexion.DisposeAsync();

    private async Task<Sala> GuardarAsync(string nombre)
    {
        var sala = new Sala(Guid.NewGuid(), nombre);

        await using var ctx = _fixture.CrearContexto();
        await new SalaRepository(ctx).AddAsync(sala);
        await new UnitOfWork(ctx).CommitAsync();
        return sala;
    }

    [Fact]
    public async Task ExisteNombreAsync_sin_distinguir_mayusculas_ni_espacios_en_los_bordes()
    {
        await GuardarAsync("Sala A");

        await using var ctx = _fixture.CrearContexto();
        var repositorio = new SalaRepository(ctx);

        Assert.True(await repositorio.ExisteNombreAsync("  SALA a  "));
        Assert.False(await repositorio.ExisteNombreAsync("Sala B"));
    }

    [Fact]
    public async Task ExisteNombreAsync_excluyendo_un_id_no_cuenta_la_propia_sala()
    {
        var sala = await GuardarAsync("Sala A");

        await using var ctx = _fixture.CrearContexto();
        var repositorio = new SalaRepository(ctx);

        Assert.False(await repositorio.ExisteNombreAsync("Sala A", excluirId: sala.Id));
        Assert.True(await repositorio.ExisteNombreAsync("Sala A", excluirId: Guid.NewGuid()));
    }
}
