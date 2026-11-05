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
/// Pruebas de <see cref="PlatoRepository"/> contra PostgreSQL real.
/// </summary>
[Collection("PostgreSQL")]
public sealed class PlatoRepositoryTests : IAsyncLifetime
{
    private static readonly Guid EspecialidadClinica = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid GrupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");

    private readonly PostgresFixture _fixture;
    private Guid _nutricionistaId;
    private Guid _platoId;
    private Guid _alimentoId;

    public PlatoRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();

        var nutricionista = new Nutricionista(Guid.NewGuid(), "Nutricionista Test", EspecialidadClinica);
        var alimento = new Alimento(
            Guid.NewGuid(), "Arroz", GrupoCereales, TipoPreparacion.Entrante.Id, NivelCalorico.Medio.Id, nutricionista.Id);
        var plato = new Plato(
            Guid.NewGuid(), "Arroz con pollo", GrupoCereales, TipoPreparacion.PlatoFuerte.Id, NivelCalorico.Alto.Id, nutricionista.Id);
        plato.AgregarIngrediente(alimento.Id, 200m);

        await using var ctx = _fixture.CrearContexto();
        ctx.AddRange(nutricionista, alimento, plato);
        await ctx.SaveChangesAsync();

        _nutricionistaId = nutricionista.Id;
        _platoId = plato.Id;
        _alimentoId = alimento.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAsync_y_GetByIdAsync_guardan_y_recuperan_un_plato()
    {
        var otroAlimento = new Alimento(
            Guid.NewGuid(), "Pollo", GrupoCereales, TipoPreparacion.PlatoFuerte.Id, NivelCalorico.Medio.Id, _nutricionistaId);
        var nuevo = new Plato(
            Guid.NewGuid(), "Pollo al horno", GrupoCereales, TipoPreparacion.PlatoFuerte.Id, NivelCalorico.Medio.Id, _nutricionistaId);
        nuevo.AgregarIngrediente(otroAlimento.Id, 150m);

        await using (var ctx = _fixture.CrearContexto())
        {
            ctx.Add(otroAlimento);
            await new PlatoRepository(ctx).AddAsync(nuevo);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        var recuperado = await new PlatoRepository(ctx2).GetByIdAsync(nuevo.Id);

        Assert.NotNull(recuperado);
        Assert.Equal("Pollo al horno", recuperado.Nombre);
    }

    [Fact]
    public async Task GetConIngredientesAsync_carga_la_composicion_del_plato()
    {
        await using var ctx = _fixture.CrearContexto();

        var plato = await new PlatoRepository(ctx).GetConIngredientesAsync(_platoId);

        Assert.NotNull(plato);
        Assert.Equal([_alimentoId], plato.Ingredientes.Select(i => i.AlimentoId));
    }

    [Fact]
    public async Task GetConIngredientesAsync_devuelve_null_si_el_plato_no_existe()
    {
        await using var ctx = _fixture.CrearContexto();

        Assert.Null(await new PlatoRepository(ctx).GetConIngredientesAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ListAsync_devuelve_todos_los_platos_sin_rastrear()
    {
        await using var ctx = _fixture.CrearContexto();

        var todos = await new PlatoRepository(ctx).ListAsync();

        Assert.Equal([_platoId], todos.Select(p => p.Id));
        Assert.Empty(ctx.ChangeTracker.Entries<Plato>());
    }

    [Fact]
    public async Task Remove_da_de_baja_un_plato_y_su_composicion()
    {
        await using (var ctx = _fixture.CrearContexto())
        {
            var repositorio = new PlatoRepository(ctx);
            repositorio.Remove((await repositorio.GetByIdAsync(_platoId))!);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        Assert.Null(await new PlatoRepository(ctx2).GetByIdAsync(_platoId));
        Assert.Empty(ctx2.PlatosIngredientes.Where(pi => pi.PlatoId == _platoId));
    }
}
