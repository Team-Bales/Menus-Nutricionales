using System;
using System.Linq;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Pruebas contra PostgreSQL real de lo que hace <see cref="ValoracionRepository"/> (#86): registrar una
/// valoración y listarlas por paciente, por dieta y por paciente y dieta a la vez, de la más a la menos reciente.
/// </summary>
[Collection("PostgreSQL")]
public sealed class GestionDeValoracionesTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _s = null!;

    public GestionDeValoracionesTests(PostgresFixture fixture)
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

    /// <summary>Dieta con un menú final, ya asignado a un paciente: la asignación que evalúa la valoración.</summary>
    private async Task<(Guid DietaId, Guid MenuId, Guid PacienteId, Guid NutricionistaId)> AsignacionAsync()
    {
        var dietaId = await _s.DietaAsync();
        var creadorId = await _s.NutricionistaAsync();
        await _s.AutorizarAsync(creadorId, dietaId);
        var menuId = await _s.MenuAsync(dietaId, creadorId);
        var revisorId = await _s.NutricionistaAsync(esJefe: true);
        await _s.AutorizarAsync(revisorId, dietaId);
        await _s.ValidarAsync(menuId, revisorId, aprobado: true);
        var pacienteId = await _s.PacienteAsync();
        await _s.AsignarAsync(menuId, pacienteId);

        return (dietaId, menuId, pacienteId, revisorId);
    }

    [Fact]
    public async Task AddAsync_persiste_la_valoracion()
    {
        var (_, menuId, pacienteId, nutricionistaId) = await AsignacionAsync();
        var valoracion = new ValoracionNutricional(
            Guid.NewGuid(), menuId, pacienteId, nutricionistaId, ResultadoNutricional.Excelente.Id, DateTime.UtcNow);

        await using var ctx = _fixture.CrearContexto();
        await new ValoracionRepository(ctx).AddAsync(valoracion);
        await new UnitOfWork(ctx).CommitAsync();

        await using var ctx2 = _fixture.CrearContexto();
        var recuperada = await new ValoracionRepository(ctx2).GetByIdAsync(valoracion.Id);
        Assert.NotNull(recuperada);
        Assert.Equal(menuId, recuperada!.MenuId);
        Assert.Equal(pacienteId, recuperada.PacienteId);
        Assert.Equal(nutricionistaId, recuperada.NutricionistaId);
        Assert.Equal(ResultadoNutricional.Excelente.Id, recuperada.ResultadoNutricionalId);
    }

    [Fact]
    public async Task GetByPacienteAsync_devuelve_las_de_todas_sus_dietas_de_la_mas_reciente_a_la_mas_antigua()
    {
        var (_, menu1, paciente, nutricionista) = await AsignacionAsync();
        var (_, menu2, _, _) = await AsignacionAsync();
        await _s.AsignarAsync(menu2, paciente);

        var antigua = await _s.ValorarAsync(menu1, paciente, nutricionista, DateTime.UtcNow.AddHours(-2));
        var reciente = await _s.ValorarAsync(menu2, paciente, nutricionista, DateTime.UtcNow.AddHours(-1));

        await using var ctx = _fixture.CrearContexto();
        var resultado = await new ValoracionRepository(ctx).GetByPacienteAsync(paciente);

        Assert.Equal([reciente, antigua], resultado.Select(v => v.Id));
    }

    [Fact]
    public async Task GetByDietaAsync_devuelve_las_de_todos_los_pacientes_de_los_menus_de_esa_dieta()
    {
        var (dietaId, menuId, pacienteA, nutricionista) = await AsignacionAsync();
        var pacienteB = await _s.PacienteAsync();
        await _s.AsignarAsync(menuId, pacienteB);
        var (_, menuDeOtraDieta, pacienteDeOtraDieta, _) = await AsignacionAsync();

        var deA = await _s.ValorarAsync(menuId, pacienteA, nutricionista, DateTime.UtcNow.AddHours(-2));
        var deB = await _s.ValorarAsync(menuId, pacienteB, nutricionista, DateTime.UtcNow.AddHours(-1));
        await _s.ValorarAsync(menuDeOtraDieta, pacienteDeOtraDieta, nutricionista, DateTime.UtcNow);

        await using var ctx = _fixture.CrearContexto();
        var resultado = await new ValoracionRepository(ctx).GetByDietaAsync(dietaId);

        Assert.Equal([deB, deA], resultado.Select(v => v.Id));
    }

    [Fact]
    public async Task GetByPacienteYDietaAsync_filtra_por_los_dos_a_la_vez()
    {
        var (dietaRenal, menuRenal, paciente, nutricionista) = await AsignacionAsync();
        var (_, menuBlanda, _, _) = await AsignacionAsync();
        await _s.AsignarAsync(menuBlanda, paciente);

        var deRenal = await _s.ValorarAsync(menuRenal, paciente, nutricionista);
        await _s.ValorarAsync(menuBlanda, paciente, nutricionista);

        await using var ctx = _fixture.CrearContexto();
        var resultado = await new ValoracionRepository(ctx).GetByPacienteYDietaAsync(paciente, dietaRenal);

        Assert.Equal([deRenal], resultado.Select(v => v.Id));
    }
}
