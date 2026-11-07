using System;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Pruebas de <see cref="EspecialidadRepository"/> contra PostgreSQL real.
/// </summary>
[Collection("PostgreSQL")]
public sealed class EspecialidadRepositoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    public EspecialidadRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ReiniciarAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExisteNombreAsync_encuentra_un_nombre_sembrado()
    {
        await using var ctx = _fixture.CrearContexto();

        Assert.True(await new EspecialidadRepository(ctx).ExisteNombreAsync("Nutrición Clínica"));
    }

    [Fact]
    public async Task ExisteNombreAsync_no_encuentra_un_nombre_inexistente()
    {
        await using var ctx = _fixture.CrearContexto();

        Assert.False(await new EspecialidadRepository(ctx).ExisteNombreAsync("Nombre que no existe"));
    }

    [Fact]
    public async Task AddAsync_y_GetByIdAsync_guardan_y_recuperan_una_especialidad()
    {
        var nueva = new Especialidad(Guid.NewGuid(), "Especialidad de prueba nueva");

        await using (var ctx = _fixture.CrearContexto())
        {
            await new EspecialidadRepository(ctx).AddAsync(nueva);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        var recuperada = await new EspecialidadRepository(ctx2).GetByIdAsync(nueva.Id);

        Assert.NotNull(recuperada);
        Assert.Equal("Especialidad de prueba nueva", recuperada.Nombre);
    }

    [Fact]
    public async Task EstaEnUsoAsync_false_para_una_especialidad_sin_nutricionistas()
    {
        var especialidad = new Especialidad(Guid.NewGuid(), "Especialidad de prueba sin uso");
        await using var ctx = _fixture.CrearContexto();
        ctx.Add(especialidad);
        await ctx.SaveChangesAsync();

        Assert.False(await new EspecialidadRepository(ctx).EstaEnUsoAsync(especialidad.Id));
    }

    [Fact]
    public async Task EstaEnUsoAsync_true_si_un_nutricionista_la_tiene()
    {
        var especialidad = new Especialidad(Guid.NewGuid(), "Especialidad de prueba con nutricionista");
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Nutricionista Test", especialidad.Id);

        await using var ctx = _fixture.CrearContexto();
        ctx.AddRange(especialidad, nutricionista);
        await ctx.SaveChangesAsync();

        Assert.True(await new EspecialidadRepository(ctx).EstaEnUsoAsync(especialidad.Id));
    }
}
