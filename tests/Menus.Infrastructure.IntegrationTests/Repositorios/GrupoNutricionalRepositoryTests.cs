using System;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Pruebas de <see cref="GrupoNutricionalRepository"/> contra PostgreSQL real.
/// </summary>
[Collection("PostgreSQL")]
public sealed class GrupoNutricionalRepositoryTests : IAsyncLifetime
{
    private static readonly Guid EspecialidadClinica = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid TipoEntrante = Guid.Parse("55555555-0000-0000-0000-000000000001");
    private static readonly Guid NivelBajo = Guid.Parse("66666666-0000-0000-0000-000000000001");
    private static readonly Guid ProgramaPediatria = Guid.Parse("33333333-0000-0000-0000-000000000001");

    private readonly PostgresFixture _fixture;

    public GrupoNutricionalRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ReiniciarAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExisteNombreAsync_encuentra_un_nombre_sembrado()
    {
        await using var ctx = _fixture.CrearContexto();

        Assert.True(await new GrupoNutricionalRepository(ctx).ExisteNombreAsync("Cereales"));
    }

    [Fact]
    public async Task ExisteNombreAsync_no_encuentra_un_nombre_inexistente()
    {
        await using var ctx = _fixture.CrearContexto();

        Assert.False(await new GrupoNutricionalRepository(ctx).ExisteNombreAsync("Nombre que no existe"));
    }

    [Fact]
    public async Task AddAsync_y_GetByIdAsync_guardan_y_recuperan_un_grupo()
    {
        var nuevo = new GrupoNutricional(Guid.NewGuid(), "Legumbres de prueba");

        await using (var ctx = _fixture.CrearContexto())
        {
            await new GrupoNutricionalRepository(ctx).AddAsync(nuevo);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        var recuperado = await new GrupoNutricionalRepository(ctx2).GetByIdAsync(nuevo.Id);

        Assert.NotNull(recuperado);
        Assert.Equal("Legumbres de prueba", recuperado.Nombre);
    }

    [Fact]
    public async Task EstaEnUsoAsync_false_para_un_grupo_sin_referencias()
    {
        var grupo = new GrupoNutricional(Guid.NewGuid(), "Grupo de prueba sin uso");
        await using var ctx = _fixture.CrearContexto();
        ctx.Add(grupo);
        await ctx.SaveChangesAsync();

        Assert.False(await new GrupoNutricionalRepository(ctx).EstaEnUsoAsync(grupo.Id));
    }

    [Fact]
    public async Task EstaEnUsoAsync_true_si_lo_usa_un_item_alimentario()
    {
        var grupo = new GrupoNutricional(Guid.NewGuid(), "Grupo de prueba con alimento");
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Nutricionista Test", EspecialidadClinica);
        var alimento = new Alimento(Guid.NewGuid(), "Alimento Test", grupo.Id, TipoEntrante, NivelBajo, nutricionista.Id);

        await using var ctx = _fixture.CrearContexto();
        ctx.AddRange(grupo, nutricionista, alimento);
        await ctx.SaveChangesAsync();

        Assert.True(await new GrupoNutricionalRepository(ctx).EstaEnUsoAsync(grupo.Id));
    }

    [Fact]
    public async Task EstaEnUsoAsync_true_si_una_dieta_lo_cubre()
    {
        var grupo = new GrupoNutricional(Guid.NewGuid(), "Grupo de prueba cubierto por dieta");
        var dieta = new Dieta(Guid.NewGuid(), "Dieta de prueba", ProgramaPediatria);
        dieta.AgregarGrupo(grupo.Id, TipoCobertura.Requerido);

        await using var ctx = _fixture.CrearContexto();
        ctx.AddRange(grupo, dieta);
        await ctx.SaveChangesAsync();

        Assert.True(await new GrupoNutricionalRepository(ctx).EstaEnUsoAsync(grupo.Id));
    }

    [Fact]
    public async Task EstaEnUsoAsync_true_si_una_restriccion_lo_excluye()
    {
        var grupo = new GrupoNutricional(Guid.NewGuid(), "Grupo de prueba excluido");
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), "Restricción de prueba");
        restriccion.ExcluirGrupo(grupo.Id);

        await using var ctx = _fixture.CrearContexto();
        ctx.AddRange(grupo, restriccion);
        await ctx.SaveChangesAsync();

        Assert.True(await new GrupoNutricionalRepository(ctx).EstaEnUsoAsync(grupo.Id));
    }
}
