using System;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Pruebas de <see cref="UnitOfWork"/> contra PostgreSQL real.
/// </summary>
[Collection("PostgreSQL")]
public sealed class UnitOfWorkTests : IAsyncLifetime
{
    private static readonly Guid EspecialidadClinica = Guid.Parse("44444444-0000-0000-0000-000000000001");

    private readonly PostgresFixture _fixture;

    public UnitOfWorkTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync() => await _fixture.ReiniciarAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CommitAsync_guarda_los_cambios_pendientes_del_contexto()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Nutricionista Test", EspecialidadClinica);

        await using var ctx = _fixture.CrearContexto();
        await ctx.Nutricionistas.AddAsync(nutricionista);

        var filas = await new UnitOfWork(ctx).CommitAsync();

        Assert.Equal(1, filas);

        await using var ctx2 = _fixture.CrearContexto();
        Assert.NotNull(await ctx2.Nutricionistas.FindAsync(nutricionista.Id));
    }

    [Fact]
    public async Task CommitAsync_no_guarda_ninguna_operacion_si_una_de_ellas_falla()
    {
        var nutricionistaValido = new Nutricionista(Guid.NewGuid(), "Nutricionista válido", EspecialidadClinica);
        var dietaConProgramaInexistente = new Dieta(Guid.NewGuid(), "Dieta inválida", Guid.NewGuid());

        await using var ctx = _fixture.CrearContexto();
        ctx.AddRange(nutricionistaValido, dietaConProgramaInexistente);

        await Assert.ThrowsAnyAsync<Exception>(() => new UnitOfWork(ctx).CommitAsync());

        await using var ctx2 = _fixture.CrearContexto();
        Assert.Null(await ctx2.Nutricionistas.FindAsync(nutricionistaValido.Id));
    }
}
