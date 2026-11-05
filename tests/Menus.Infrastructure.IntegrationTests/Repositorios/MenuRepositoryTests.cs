using System;
using System.Linq;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Pruebas de <see cref="MenuRepository"/> contra PostgreSQL real.
/// </summary>
/// <remarks>
/// Van contra PostgreSQL y no contra la base en memoria por dos motivos: los filtros por estado tienen que
/// traducirse a SQL (EF no traduce <see cref="Menu.EsFinal"/> ni <see cref="Menu.EsPendiente"/>), y los triggers
/// de RS-07 y RS-15 tienen que aceptar cómo guarda EF el agregado.
/// Cada prueba siembra tres menús de la misma dieta: uno aprobado y asignado a P1, uno pendiente y uno rechazado.
/// </remarks>
[Collection("PostgreSQL")]
public sealed class MenuRepositoryTests : IAsyncLifetime
{
    private static readonly Guid EspecialidadClinica = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid ProgramaPediatria = Guid.Parse("33333333-0000-0000-0000-000000000001");
    private static readonly Guid GrupoFrutas = Guid.Parse("11111111-0000-0000-0000-000000000004");

    private readonly PostgresFixture _fixture;
    private Escenario _e = null!;

    public MenuRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();
        _e = await SembrarAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetFinalesByDietaAsync_devuelve_solo_los_aprobados()
    {
        await using var ctx = _fixture.CrearContexto();

        var finales = await new MenuRepository(ctx).GetFinalesByDietaAsync(_e.DietaId);

        Assert.Equal([_e.FinalId], finales.Select(m => m.Id));
    }

    [Fact]
    public async Task GetPendientesByDietaAsync_devuelve_solo_los_que_no_tienen_dictamen()
    {
        await using var ctx = _fixture.CrearContexto();

        var pendientes = await new MenuRepository(ctx).GetPendientesByDietaAsync(_e.DietaId);

        Assert.Equal([_e.PendienteId], pendientes.Select(m => m.Id));
    }

    [Fact]
    public async Task GetByDietaAsync_trae_los_menus_con_su_parametrizacion_y_sin_rastrear()
    {
        await using var ctx = _fixture.CrearContexto();

        var menus = await new MenuRepository(ctx).GetByDietaAsync(_e.DietaId);

        Assert.Equal(3, menus.Count);
        Assert.All(menus, m =>
        {
            Assert.Single(m.Distribuciones);
            Assert.Single(m.GruposNutricionales);
        });
        Assert.Empty(ctx.ChangeTracker.Entries<Menu>());
    }

    [Fact]
    public async Task ListAsync_y_GetByNutricionistaCreadorAsync_devuelven_los_tres_menus()
    {
        await using var ctx = _fixture.CrearContexto();
        var repositorio = new MenuRepository(ctx);

        Assert.Equal(3, (await repositorio.ListAsync()).Count);
        Assert.Equal(3, (await repositorio.GetByNutricionistaCreadorAsync(_e.CreadorId)).Count);
        Assert.Empty(await repositorio.GetByNutricionistaCreadorAsync(_e.JefeId));
    }

    [Fact]
    public async Task GetByPacienteAsync_devuelve_los_menus_asignados_al_paciente()
    {
        await using var ctx = _fixture.CrearContexto();
        var repositorio = new MenuRepository(ctx);

        Assert.Equal([_e.FinalId], (await repositorio.GetByPacienteAsync(_e.P1)).Select(m => m.Id));
        Assert.Empty(await repositorio.GetByPacienteAsync(_e.P2));
    }

    [Fact]
    public async Task GetByIdAsync_carga_el_agregado_completo()
    {
        await using var ctx = _fixture.CrearContexto();

        var menu = await new MenuRepository(ctx).GetByIdAsync(_e.FinalId);

        Assert.NotNull(menu);
        Assert.Single(menu.Items);
        Assert.Single(menu.Distribuciones);
        Assert.Single(menu.GruposNutricionales);
        Assert.True(menu.EsFinal);
        Assert.Single(menu.Asignaciones);
    }

    [Fact]
    public async Task GetByIdAsync_deja_el_agregado_rastreado_y_sus_cambios_se_guardan()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        await using (var ctx = _fixture.CrearContexto())
        {
            var menu = (await new MenuRepository(ctx).GetByIdAsync(_e.FinalId))!;

            // Con las asignaciones cargadas, el dominio ve que P1 ya estaba asignado.
            Assert.Throws<ReglaDeNegocioException>(() => menu.AsignarPaciente(_e.P1));

            menu.AsignarPaciente(_e.P2);
            menu.RegistrarServicio(_e.P1, hoy);
            menu.RegistrarConsumo(_e.P1, hoy, _e.AlimentoId, true);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CrearContexto())
        {
            var menu = (await new MenuRepository(ctx).GetByIdAsync(_e.FinalId))!;
            var servicios = menu.Asignaciones.SelectMany(a => a.Servicios).ToList();

            Assert.Equal(2, menu.Asignaciones.Count);
            Assert.Single(servicios);
            Assert.Single(servicios.SelectMany(s => s.Consumos));
        }
    }

    [Fact]
    public async Task GetByIdAsync_devuelve_null_si_el_menu_no_existe()
    {
        await using var ctx = _fixture.CrearContexto();

        Assert.Null(await new MenuRepository(ctx).GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AddAsync_y_Remove_dan_de_alta_y_de_baja_un_menu()
    {
        var nuevo = NuevoMenu(_e.DietaId, _e.CreadorId, _e.AlimentoId);
        await using (var ctx = _fixture.CrearContexto())
        {
            await new MenuRepository(ctx).AddAsync(nuevo);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CrearContexto())
        {
            var repositorio = new MenuRepository(ctx);
            Assert.Equal(2, (await repositorio.GetPendientesByDietaAsync(_e.DietaId)).Count);

            repositorio.Remove((await repositorio.GetByIdAsync(nuevo.Id))!);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CrearContexto())
        {
            Assert.Null(await new MenuRepository(ctx).GetByIdAsync(nuevo.Id));
        }
    }

    /// <summary>
    /// Menú pendiente, con su parametrización (RS-07 exige darla al crearlo) y un ítem.
    /// </summary>
    private static Menu NuevoMenu(Guid dietaId, Guid creadorId, Guid alimentoId)
    {
        var menu = new Menu(Guid.NewGuid(), dietaId, creadorId, DateTime.UtcNow.AddDays(-6), 1, true,
            [(TipoPreparacion.Postre.Id, 1m)], [GrupoFrutas]);
        menu.AgregarItem(alimentoId);
        return menu;
    }

    private async Task<Escenario> SembrarAsync()
    {
        var creador = new Nutricionista(Guid.NewGuid(), "Creadora", EspecialidadClinica);
        var jefe = new Nutricionista(Guid.NewGuid(), "Jefe", EspecialidadClinica);
        var dieta = new Dieta(Guid.NewGuid(), "Dieta de prueba", ProgramaPediatria);
        dieta.AgregarGrupo(GrupoFrutas, TipoCobertura.Requerido);
        creador.AutorizarDieta(dieta.Id);
        jefe.AutorizarDieta(dieta.Id);
        jefe.DesignarJefe();
        var sala = new Sala(Guid.NewGuid(), "Sala de prueba");
        var p1 = new Paciente(Guid.NewGuid(), "P1", 30, sala.Id);
        p1.InscribirDieta(dieta.Id);
        var p2 = new Paciente(Guid.NewGuid(), "P2", 30, sala.Id);
        p2.InscribirDieta(dieta.Id);
        var alimento = new Alimento(Guid.NewGuid(), "Manzana", GrupoFrutas, TipoPreparacion.Postre.Id, NivelCalorico.Bajo.Id, creador.Id);
        var final = NuevoMenu(dieta.Id, creador.Id, alimento.Id);
        var pendiente = NuevoMenu(dieta.Id, creador.Id, alimento.Id);
        var rechazado = NuevoMenu(dieta.Id, creador.Id, alimento.Id);

        await using (var ctx = _fixture.CrearContexto())
        {
            ctx.AddRange(creador, jefe, dieta, sala, p1, p2, alimento, final, pendiente, rechazado);
            await ctx.SaveChangesAsync();
        }

        // El dictamen va en otra transacción: RS-15 no deja tocar los ítems de un menú dictaminado en la misma.
        var fechaDictamen = DateTime.UtcNow.AddDays(-3);
        await using (var ctx = _fixture.CrearContexto())
        {
            var repositorio = new MenuRepository(ctx);
            var aprobado = (await repositorio.GetByIdAsync(final.Id))!;
            aprobado.Aprobar(jefe.Id, fechaDictamen);
            aprobado.AsignarPaciente(p1.Id);
            (await repositorio.GetByIdAsync(rechazado.Id))!.Rechazar(jefe.Id, fechaDictamen, "No cumple.");
            await ctx.SaveChangesAsync();
        }

        return new Escenario(dieta.Id, creador.Id, jefe.Id, p1.Id, p2.Id, alimento.Id, final.Id, pendiente.Id);
    }

    private sealed record Escenario(
        Guid DietaId, Guid CreadorId, Guid JefeId, Guid P1, Guid P2, Guid AlimentoId, Guid FinalId, Guid PendienteId);
}
