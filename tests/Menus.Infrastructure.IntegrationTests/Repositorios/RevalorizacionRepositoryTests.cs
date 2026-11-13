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
/// Pruebas de <see cref="RevalorizacionRepository"/> contra PostgreSQL real.
/// </summary>
[Collection("PostgreSQL")]
public sealed class RevalorizacionRepositoryTests : IAsyncLifetime
{
    private static readonly Guid EspecialidadClinica = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid EspecialidadOtra = Guid.Parse("44444444-0000-0000-0000-000000000002");
    private static readonly Guid GrupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid TipoPlatoFuerte = Guid.Parse("55555555-0000-0000-0000-000000000002");
    private static readonly Guid NivelMedio = Guid.Parse("66666666-0000-0000-0000-000000000002");
    private static readonly Guid ProgramaPediatria = Guid.Parse("33333333-0000-0000-0000-000000000001");

    private readonly PostgresFixture _fixture;
    private Guid _nutricionistaEjecutorId;
    private Guid _nutricionistaOtroId;
    private Guid _pacienteId;
    private Guid _menuId;
    private Guid _valoracionRevisadaId;
    private Guid _salaId;

    public RevalorizacionRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();

        // Crear sala
        var sala = new Sala(Guid.NewGuid(), "Sala Test");
        _salaId = sala.Id;

        // Crear nutricionistas
        var nutricionistaEjecutor = new Nutricionista(Guid.NewGuid(), "Nutricionista Ejecutor", EspecialidadClinica);
        var nutricionistaOtro = new Nutricionista(Guid.NewGuid(), "Nutricionista Otro", EspecialidadOtra);
        _nutricionistaEjecutorId = nutricionistaEjecutor.Id;
        _nutricionistaOtroId = nutricionistaOtro.Id;

        // Crear paciente
        var paciente = new Paciente(Guid.NewGuid(), "Paciente Test", 30, _salaId);
        _pacienteId = paciente.Id;

        // Crear dieta
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Test", ProgramaPediatria);
        dieta.AgregarGrupo(GrupoCereales, TipoCobertura.Requerido);
        _ = dieta.Id; // suppress unused warning

        // Autorizar nutricionistas para la dieta
        nutricionistaEjecutor.AutorizarDieta(dieta.Id);
        nutricionistaOtro.AutorizarDieta(dieta.Id);

        // Crear menú (aprobado)
        var menu = new Menu(
            Guid.NewGuid(),
            dieta.Id,
            _nutricionistaEjecutorId,
            DateTime.UtcNow.AddDays(-2),
            1,
            false,
            [(TipoPlatoFuerte, 1.0m)],
            [GrupoCereales]);
        _menuId = menu.Id;

        // Aprobar el menú (para que sea final y se pueda asignar)
        var nutricionistaJefe = new Nutricionista(Guid.NewGuid(), "Jefe Nutricion", EspecialidadClinica);
        nutricionistaJefe.AutorizarDieta(dieta.Id);
        nutricionistaJefe.DesignarJefe();
        menu.Aprobar(nutricionistaJefe.Id, DateTime.UtcNow.AddDays(-1));

        // Asignar menú al paciente
        menu.AsignarPaciente(_pacienteId);

        // Crear valoración revisada
        var valoracionRevisada = new ValoracionNutricional(
            Guid.NewGuid(),
            _menuId,
            _pacienteId,
            _nutricionistaEjecutorId,
            ResultadoNutricional.Normal.Id,
            DateTime.UtcNow.AddDays(-1));
        _valoracionRevisadaId = valoracionRevisada.Id;

        await using var ctx = _fixture.CrearContexto();
        ctx.AddRange(sala, nutricionistaEjecutor, nutricionistaOtro, nutricionistaJefe, paciente, dieta, menu, valoracionRevisada);
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetPendientesByNutricionistaAsync_devuelve_solo_las_del_nutricionista_sin_valoracion_generada_ordenadas_por_fecha()
    {
        var r1 = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow.AddHours(-2), _nutricionistaEjecutorId, _valoracionRevisadaId);
        var r2 = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow.AddHours(-1), _nutricionistaEjecutorId, _valoracionRevisadaId);
        var r3 = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow, _nutricionistaOtroId, _valoracionRevisadaId);
        var r4 = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow.AddHours(-3), _nutricionistaEjecutorId, _valoracionRevisadaId);
        r4.RegistrarValoracionGenerada(Guid.NewGuid()); // Ya atendida

        await using (var ctx = _fixture.CrearContexto())
        {
            ctx.AddRange(r1, r2, r3, r4);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        var pendientes = await new RevalorizacionRepository(ctx2).GetPendientesByNutricionistaAsync(_nutricionistaEjecutorId);

        Assert.Equal(2, pendientes.Count);
        Assert.Equal(r1.Id, pendientes[0].Id); // Más antigua primero
        Assert.Equal(r2.Id, pendientes[1].Id);
        Assert.All(pendientes, r => Assert.Null(r.ValoracionGeneradaId));
        Assert.All(pendientes, r => Assert.Equal(_nutricionistaEjecutorId, r.NutricionistaId));
    }

    [Fact]
    public async Task GetByPacienteAsync_devuelve_las_revalorizaciones_que_revisan_sus_valoraciones()
    {
        var r1 = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow.AddHours(-2), _nutricionistaEjecutorId, _valoracionRevisadaId);
        var r2 = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow.AddHours(-1), _nutricionistaOtroId, _valoracionRevisadaId);
        var r3 = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow, _nutricionistaEjecutorId, Guid.NewGuid()); // De otro paciente

        await using (var ctx = _fixture.CrearContexto())
        {
            ctx.AddRange(r1, r2, r3);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = _fixture.CrearContexto();
        var porPaciente = await new RevalorizacionRepository(ctx2).GetByPacienteAsync(_pacienteId);

        Assert.Equal(2, porPaciente.Count);
        Assert.Contains(porPaciente, r => r.Id == r1.Id);
        Assert.Contains(porPaciente, r => r.Id == r2.Id);
        Assert.DoesNotContain(porPaciente, r => r.Id == r3.Id);
    }
}
