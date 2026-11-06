using System;
using System.Linq;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Pruebas de <see cref="AlimentoRepository"/> contra PostgreSQL real.
/// </summary>
[Collection("PostgreSQL")]
public sealed class AlimentoRepositoryTests : IAsyncLifetime
{
    private static readonly Guid EspecialidadClinica = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid GrupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid GrupoFrutas = Guid.Parse("11111111-0000-0000-0000-000000000004");

    private readonly PostgresFixture _fixture;
    private Guid _nutricionistaId;
    private Guid _cerealBajoId;
    private Guid _frutaAltaId;

    public AlimentoRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();

        var nutricionista = new Nutricionista(Guid.NewGuid(), "Nutricionista Test", EspecialidadClinica);
        var cerealBajo = new Alimento(
            Guid.NewGuid(), "Avena", GrupoCereales, TipoPreparacion.Entrante.Id, NivelCalorico.Bajo.Id, nutricionista.Id);
        var frutaAlta = new Alimento(
            Guid.NewGuid(), "Aguacate", GrupoFrutas, TipoPreparacion.Postre.Id, NivelCalorico.Alto.Id, nutricionista.Id);

        await using var ctx = _fixture.CrearContexto();
        ctx.AddRange(nutricionista, cerealBajo, frutaAlta);
        await ctx.SaveChangesAsync();

        _nutricionistaId = nutricionista.Id;
        _cerealBajoId = cerealBajo.Id;
        _frutaAltaId = frutaAlta.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAsync_y_GetByIdAsync_guardan_y_recuperan_un_alimento()
    {
        var nuevo = new Alimento(
            Guid.NewGuid(), "Quinoa", GrupoCereales, TipoPreparacion.Entrante.Id, NivelCalorico.Medio.Id, _nutricionistaId);

        await using (var ctx = _fixture.CrearContexto())
        {
            await new AlimentoRepository(ctx).AddAsync(nuevo);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        var recuperado = await new AlimentoRepository(ctx2).GetByIdAsync(nuevo.Id);

        Assert.NotNull(recuperado);
        Assert.Equal("Quinoa", recuperado.Nombre);
    }

    [Fact]
    public async Task GetByIdAsync_devuelve_null_si_el_alimento_no_existe()
    {
        await using var ctx = _fixture.CrearContexto();

        Assert.Null(await new AlimentoRepository(ctx).GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByGrupoNutricionalAsync_filtra_por_grupo()
    {
        await using var ctx = _fixture.CrearContexto();

        var cereales = await new AlimentoRepository(ctx).GetByGrupoNutricionalAsync(GrupoCereales);

        Assert.Equal([_cerealBajoId], cereales.Select(a => a.Id));
    }

    [Fact]
    public async Task GetByNivelCaloricoAsync_filtra_por_nivel()
    {
        await using var ctx = _fixture.CrearContexto();

        var altos = await new AlimentoRepository(ctx).GetByNivelCaloricoAsync(NivelCalorico.Alto);

        Assert.Equal([_frutaAltaId], altos.Select(a => a.Id));
    }

    [Fact]
    public async Task ListAsync_devuelve_todos_los_alimentos_sin_rastrear()
    {
        await using var ctx = _fixture.CrearContexto();

        var todos = await new AlimentoRepository(ctx).ListAsync();

        Assert.Equal(2, todos.Count);
        Assert.Empty(ctx.ChangeTracker.Entries<Alimento>());
    }

    [Fact]
    public async Task Remove_da_de_baja_un_alimento()
    {
        await using (var ctx = _fixture.CrearContexto())
        {
            var repositorio = new AlimentoRepository(ctx);
            repositorio.Remove((await repositorio.GetByIdAsync(_cerealBajoId))!);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        Assert.Null(await new AlimentoRepository(ctx2).GetByIdAsync(_cerealBajoId));
    }
}
