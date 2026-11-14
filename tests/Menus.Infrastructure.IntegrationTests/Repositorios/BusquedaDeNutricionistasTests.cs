using System;
using System.Linq;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>Pruebas contra PostgreSQL real de <see cref="NutricionistaRepository.BuscarAsync"/> (#71).</summary>
[Collection("PostgreSQL")]
public sealed class BusquedaDeNutricionistasTests : IAsyncLifetime
{
    private static readonly Guid Clinica = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid DieteticaHospitalaria = Guid.Parse("44444444-0000-0000-0000-000000000002");

    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;

    public BusquedaDeNutricionistasTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();
        _conexion = await _fixture.AbrirAsync();
    }

    public async Task DisposeAsync() => await _conexion.DisposeAsync();

    private async Task<Nutricionista> GuardarAsync(string nombre, Guid especialidadId)
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), nombre, especialidadId);

        await using var ctx = _fixture.CrearContexto();
        await new NutricionistaRepository(ctx).AddAsync(nutricionista);
        await new UnitOfWork(ctx).CommitAsync();
        return nutricionista;
    }

    [Fact]
    public async Task BuscarAsync_filtra_por_especialidad_y_ordena_por_nombre()
    {
        await GuardarAsync("Carla", Clinica);
        await GuardarAsync("Ana", Clinica);
        await GuardarAsync("Beto", DieteticaHospitalaria);

        await using var ctx = _fixture.CrearContexto();
        var repositorio = new NutricionistaRepository(ctx);

        var (deClinica, totalClinica) = await repositorio.BuscarAsync(Clinica, 1, 20);
        var (todos, totalTodos) = await repositorio.BuscarAsync(null, 1, 20);

        Assert.Equal(["Ana", "Carla"], deClinica.Select(n => n.Nombre));
        Assert.Equal(2, totalClinica);
        Assert.Equal(["Ana", "Beto", "Carla"], todos.Select(n => n.Nombre));
        Assert.Equal(3, totalTodos);
    }

    [Fact]
    public async Task BuscarAsync_pagina_sin_rastrear_y_da_el_total_sin_paginar()
    {
        await GuardarAsync("Ana", Clinica);
        await GuardarAsync("Beto", Clinica);
        await GuardarAsync("Carla", Clinica);

        await using var ctx = _fixture.CrearContexto();
        var (pagina, total) = await new NutricionistaRepository(ctx).BuscarAsync(null, 2, 2);

        Assert.Equal(["Carla"], pagina.Select(n => n.Nombre));
        Assert.Equal(3, total);
        Assert.Empty(ctx.ChangeTracker.Entries<Nutricionista>());
    }
}
