using System;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Pruebas de <see cref="RestriccionAlimentariaRepository"/> contra PostgreSQL real.
/// </summary>
[Collection("PostgreSQL")]
public sealed class RestriccionAlimentariaRepositoryTests : IAsyncLifetime
{
    private static readonly Guid GrupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid ProgramaPediatria = Guid.Parse("33333333-0000-0000-0000-000000000001");

    private readonly PostgresFixture _fixture;

    public RestriccionAlimentariaRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ReiniciarAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExisteNombreAsync_encuentra_un_nombre_sembrado()
    {
        await using var ctx = _fixture.CrearContexto();

        Assert.True(await new RestriccionAlimentariaRepository(ctx).ExisteNombreAsync("Sin gluten"));
    }

    [Fact]
    public async Task ExisteNombreAsync_no_encuentra_un_nombre_inexistente()
    {
        await using var ctx = _fixture.CrearContexto();

        Assert.False(await new RestriccionAlimentariaRepository(ctx).ExisteNombreAsync("Nombre que no existe"));
    }

    [Fact]
    public async Task AddAsync_y_GetByIdAsync_guardan_y_recuperan_una_restriccion()
    {
        var nueva = new RestriccionAlimentaria(Guid.NewGuid(), "Restricción de prueba nueva");

        await using (var ctx = _fixture.CrearContexto())
        {
            await new RestriccionAlimentariaRepository(ctx).AddAsync(nueva);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        var recuperada = await new RestriccionAlimentariaRepository(ctx2).GetByIdAsync(nueva.Id);

        Assert.NotNull(recuperada);
        Assert.Equal("Restricción de prueba nueva", recuperada.Nombre);
    }

    [Fact]
    public async Task EstaEnUsoAsync_false_para_una_restriccion_sin_referencias()
    {
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), "Restricción de prueba sin uso");
        await using var ctx = _fixture.CrearContexto();
        ctx.Add(restriccion);
        await ctx.SaveChangesAsync();

        Assert.False(await new RestriccionAlimentariaRepository(ctx).EstaEnUsoAsync(restriccion.Id));
    }

    [Fact]
    public async Task EstaEnUsoAsync_true_si_una_dieta_la_aplica()
    {
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), "Restricción aplicada por dieta");
        var dieta = new Dieta(Guid.NewGuid(), "Dieta de prueba", ProgramaPediatria);
        dieta.AgregarGrupo(GrupoCereales, TipoCobertura.Requerido);
        dieta.AgregarRestriccion(restriccion.Id);

        await using var ctx = _fixture.CrearContexto();
        ctx.AddRange(restriccion, dieta);
        await ctx.SaveChangesAsync();

        Assert.True(await new RestriccionAlimentariaRepository(ctx).EstaEnUsoAsync(restriccion.Id));
    }

    [Fact]
    public async Task EstaEnUsoAsync_true_si_ella_misma_excluye_un_grupo()
    {
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), "Restricción con exclusión propia");
        restriccion.ExcluirGrupo(GrupoCereales);

        await using var ctx = _fixture.CrearContexto();
        ctx.Add(restriccion);
        await ctx.SaveChangesAsync();

        Assert.True(await new RestriccionAlimentariaRepository(ctx).EstaEnUsoAsync(restriccion.Id));
    }
}
