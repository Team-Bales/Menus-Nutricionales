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
/// Pruebas de <see cref="DietaRepository"/> contra PostgreSQL real.
/// </summary>
[Collection("PostgreSQL")]
public sealed class DietaRepositoryTests : IAsyncLifetime
{
    private static readonly Guid ProgramaPediatria = Guid.Parse("33333333-0000-0000-0000-000000000001");
    private static readonly Guid ProgramaOncologia = Guid.Parse("33333333-0000-0000-0000-000000000002");
    private static readonly Guid GrupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid RestriccionSinGluten = Guid.Parse("22222222-0000-0000-0000-000000000001");

    private readonly PostgresFixture _fixture;
    private Guid _dietaPediatricaId;
    private Guid _dietaOncologicaId;

    public DietaRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();

        var pediatrica = new Dieta(Guid.NewGuid(), "Dieta pediátrica de prueba", ProgramaPediatria);
        pediatrica.AgregarGrupo(GrupoCereales, TipoCobertura.Requerido);
        pediatrica.AgregarRestriccion(RestriccionSinGluten);

        var oncologica = new Dieta(Guid.NewGuid(), "Dieta oncológica de prueba", ProgramaOncologia);
        oncologica.AgregarGrupo(GrupoCereales, TipoCobertura.Requerido);

        await using var ctx = _fixture.CrearContexto();
        ctx.AddRange(pediatrica, oncologica);
        await ctx.SaveChangesAsync();

        _dietaPediatricaId = pediatrica.Id;
        _dietaOncologicaId = oncologica.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAsync_y_GetByIdAsync_guardan_y_recuperan_una_dieta()
    {
        var nueva = new Dieta(Guid.NewGuid(), "Dieta geriátrica de prueba", ProgramaOncologia);
        nueva.AgregarGrupo(GrupoCereales, TipoCobertura.Requerido);

        await using (var ctx = _fixture.CrearContexto())
        {
            await new DietaRepository(ctx).AddAsync(nueva);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        var recuperada = await new DietaRepository(ctx2).GetByIdAsync(nueva.Id);

        Assert.NotNull(recuperada);
        Assert.Equal("Dieta geriátrica de prueba", recuperada.Nombre);
    }

    [Fact]
    public async Task GetConGruposYRestriccionesAsync_carga_las_dos_colecciones()
    {
        await using var ctx = _fixture.CrearContexto();

        var dieta = await new DietaRepository(ctx).GetConGruposYRestriccionesAsync(_dietaPediatricaId);

        Assert.NotNull(dieta);
        Assert.Equal([GrupoCereales], dieta.GruposNutricionales.Select(g => g.GrupoNutricionalId));
        Assert.Equal([RestriccionSinGluten], dieta.Restricciones.Select(r => r.RestriccionAlimentariaId));
    }

    [Fact]
    public async Task GetConGruposYRestriccionesAsync_devuelve_null_si_la_dieta_no_existe()
    {
        await using var ctx = _fixture.CrearContexto();

        Assert.Null(await new DietaRepository(ctx).GetConGruposYRestriccionesAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByProgramaAtencionAsync_filtra_por_programa()
    {
        await using var ctx = _fixture.CrearContexto();

        var pediatricas = await new DietaRepository(ctx).GetByProgramaAtencionAsync(ProgramaPediatria);

        Assert.Equal([_dietaPediatricaId], pediatricas.Select(d => d.Id));
    }

    [Fact]
    public async Task ListAsync_devuelve_todas_las_dietas_sin_rastrear()
    {
        await using var ctx = _fixture.CrearContexto();

        var todas = await new DietaRepository(ctx).ListAsync();

        Assert.Equal(2, todas.Count);
        Assert.Empty(ctx.ChangeTracker.Entries<Dieta>());
    }

    [Fact]
    public async Task Remove_da_de_baja_una_dieta_y_su_cobertura()
    {
        await using (var ctx = _fixture.CrearContexto())
        {
            var repositorio = new DietaRepository(ctx);
            repositorio.Remove((await repositorio.GetByIdAsync(_dietaOncologicaId))!);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        Assert.Null(await new DietaRepository(ctx2).GetByIdAsync(_dietaOncologicaId));
    }
}
