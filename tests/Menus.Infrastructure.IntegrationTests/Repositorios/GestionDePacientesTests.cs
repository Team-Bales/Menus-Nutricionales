using System;
using System.Linq;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Pruebas contra PostgreSQL real de lo que hacen los casos de uso de Pacientes (#73) con EF: el listado filtrado y
/// paginado, actualizar y trasladar a un paciente rastreado, inscribirlo y desinscribirlo, y el nombre de sus dietas.
/// </summary>
[Collection("PostgreSQL")]
public sealed class GestionDePacientesTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _s = null!;

    public GestionDePacientesTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();
        _conexion = await _fixture.AbrirAsync();
        _s = new Sembrador(_conexion);
    }

    public async Task DisposeAsync() => await _conexion.DisposeAsync();

    private async Task<Paciente> GuardarAsync(string nombre, Guid salaId, params Guid[] dietas)
    {
        var paciente = new Paciente(Guid.NewGuid(), nombre, 30, salaId);
        foreach (var dieta in dietas)
        {
            paciente.InscribirDieta(dieta);
        }

        await using var ctx = _fixture.CrearContexto();
        await new PacienteRepository(ctx).AddAsync(paciente);
        await new UnitOfWork(ctx).CommitAsync();
        return paciente;
    }

    [Fact]
    public async Task BuscarAsync_filtra_por_sala_y_por_dieta_y_ordena_por_nombre()
    {
        var salaA = await _s.SalaAsync();
        var salaB = await _s.SalaAsync();
        var renal = await _s.DietaAsync();
        var blanda = await _s.DietaAsync();
        var carla = await GuardarAsync("Carla", salaA, renal);
        var ana = await GuardarAsync("Ana", salaA, renal, blanda);
        await GuardarAsync("Beto", salaB, renal);
        await GuardarAsync("Dora", salaA, blanda);

        await using var ctx = _fixture.CrearContexto();
        var repositorio = new PacienteRepository(ctx);

        var (deSalaA, totalSalaA) = await repositorio.BuscarAsync(salaA, null, 1, 20);
        var (deRenal, totalRenal) = await repositorio.BuscarAsync(null, renal, 1, 20);
        var (ambos, totalAmbos) = await repositorio.BuscarAsync(salaA, renal, 1, 20);

        Assert.Equal(["Ana", "Carla", "Dora"], deSalaA.Select(p => p.Nombre));
        Assert.Equal(3, totalSalaA);
        Assert.Equal(["Ana", "Beto", "Carla"], deRenal.Select(p => p.Nombre));
        Assert.Equal(3, totalRenal);
        Assert.Equal([ana.Id, carla.Id], ambos.Select(p => p.Id));
        Assert.Equal(2, totalAmbos);
    }

    [Fact]
    public async Task BuscarAsync_pagina_sin_rastrear_y_da_el_total_sin_paginar()
    {
        var sala = await _s.SalaAsync();
        await GuardarAsync("Ana", sala);
        await GuardarAsync("Beto", sala);
        await GuardarAsync("Carla", sala);

        await using var ctx = _fixture.CrearContexto();
        var (pagina, total) = await new PacienteRepository(ctx).BuscarAsync(null, null, 2, 2);

        Assert.Equal(["Carla"], pagina.Select(p => p.Nombre));
        Assert.Equal(3, total);
        Assert.Empty(ctx.ChangeTracker.Entries<Paciente>());
    }

    [Fact]
    public async Task Actualizar_y_trasladar_un_paciente_rastreado_persiste_el_cambio()
    {
        var salaA = await _s.SalaAsync();
        var salaB = await _s.SalaAsync();
        var paciente = await GuardarAsync("Ana", salaA);

        await using (var ctx = _fixture.CrearContexto())
        {
            var rastreado = (await new PacienteRepository(ctx).GetByIdAsync(paciente.Id))!;
            rastreado.ActualizarDatos("Ana María", 0);
            rastreado.TrasladarASala(salaB);
            await new UnitOfWork(ctx).CommitAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        var recuperado = (await new PacienteRepository(ctx2).GetByIdAsync(paciente.Id))!;
        Assert.Equal("Ana María", recuperado.Nombre);
        Assert.Equal(0, recuperado.Edad);
        Assert.Equal(salaB, recuperado.SalaId);
    }

    [Fact]
    public async Task Inscribir_y_desinscribir_un_paciente_rastreado_persiste_el_cambio()
    {
        var sala = await _s.SalaAsync();
        var renal = await _s.DietaAsync();
        var blanda = await _s.DietaAsync();
        var paciente = await GuardarAsync("Ana", sala, renal);

        await using (var ctx = _fixture.CrearContexto())
        {
            var rastreado = (await new PacienteRepository(ctx).GetByIdAsync(paciente.Id))!;
            rastreado.InscribirDieta(blanda);
            rastreado.DesinscribirDieta(renal);
            await new UnitOfWork(ctx).CommitAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        var recuperado = (await new PacienteRepository(ctx2).GetByIdAsync(paciente.Id))!;
        Assert.Equal([blanda], recuperado.Dietas.Select(d => d.DietaId));
    }

    [Fact]
    public async Task GetByIdsAsync_de_dietas_devuelve_solo_las_que_existen_sin_rastrear()
    {
        var renal = await _s.DietaAsync();
        var blanda = await _s.DietaAsync();
        await _s.DietaAsync();

        await using var ctx = _fixture.CrearContexto();
        var dietas = await new DietaRepository(ctx).GetByIdsAsync([renal, blanda, Guid.NewGuid()]);

        Assert.Equal(
            new[] { renal, blanda }.OrderBy(id => id),
            dietas.Select(d => d.Id).OrderBy(id => id));
        Assert.Empty(ctx.ChangeTracker.Entries<Dieta>());
    }
}
