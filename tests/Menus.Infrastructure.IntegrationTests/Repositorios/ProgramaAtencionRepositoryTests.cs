using System;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Pruebas de <see cref="ProgramaAtencionRepository"/> contra PostgreSQL real.
/// </summary>
[Collection("PostgreSQL")]
public sealed class ProgramaAtencionRepositoryTests : IAsyncLifetime
{
    private static readonly Guid GrupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");

    private readonly PostgresFixture _fixture;

    public ProgramaAtencionRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ReiniciarAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExisteNombreAsync_encuentra_un_nombre_sembrado()
    {
        await using var ctx = _fixture.CrearContexto();

        Assert.True(await new ProgramaAtencionRepository(ctx).ExisteNombreAsync("Nutrición Pediátrica"));
    }

    [Fact]
    public async Task ExisteNombreAsync_no_encuentra_un_nombre_inexistente()
    {
        await using var ctx = _fixture.CrearContexto();

        Assert.False(await new ProgramaAtencionRepository(ctx).ExisteNombreAsync("Nombre que no existe"));
    }

    [Fact]
    public async Task AddAsync_y_GetByIdAsync_guardan_y_recuperan_un_programa()
    {
        var nuevo = new ProgramaAtencion(Guid.NewGuid(), "Programa de prueba nuevo");

        await using (var ctx = _fixture.CrearContexto())
        {
            await new ProgramaAtencionRepository(ctx).AddAsync(nuevo);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        var recuperado = await new ProgramaAtencionRepository(ctx2).GetByIdAsync(nuevo.Id);

        Assert.NotNull(recuperado);
        Assert.Equal("Programa de prueba nuevo", recuperado.Nombre);
    }

    [Fact]
    public async Task EstaEnUsoAsync_false_para_un_programa_sin_dietas()
    {
        var programa = new ProgramaAtencion(Guid.NewGuid(), "Programa de prueba sin uso");
        await using var ctx = _fixture.CrearContexto();
        ctx.Add(programa);
        await ctx.SaveChangesAsync();

        Assert.False(await new ProgramaAtencionRepository(ctx).EstaEnUsoAsync(programa.Id));
    }

    [Fact]
    public async Task EstaEnUsoAsync_true_si_una_dieta_pertenece_al_programa()
    {
        var programa = new ProgramaAtencion(Guid.NewGuid(), "Programa de prueba con dieta");
        var dieta = new Dieta(Guid.NewGuid(), "Dieta de prueba", programa.Id);
        dieta.AgregarGrupo(GrupoCereales, TipoCobertura.Requerido);

        await using var ctx = _fixture.CrearContexto();
        ctx.AddRange(programa, dieta);
        await ctx.SaveChangesAsync();

        Assert.True(await new ProgramaAtencionRepository(ctx).EstaEnUsoAsync(programa.Id));
    }
}
