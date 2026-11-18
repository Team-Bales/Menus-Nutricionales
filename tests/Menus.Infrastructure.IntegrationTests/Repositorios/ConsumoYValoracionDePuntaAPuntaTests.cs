using System;
using System.Threading.Tasks;
using Menus.Application.Comun;
using Menus.Application.GestionDeMenus.RegistrarServicio;
using Menus.Application.Revalorizaciones.RegistrarResultadoRevalorizacion;
using Menus.Application.Revalorizaciones.SolicitarRevalorizacion;
using Menus.Application.Valoraciones.RegistrarValoracion;
using Menus.Domain;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.Cache;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Prueba de integración de punta a punta (#89): asigna un menú, registra su servicio y el consumo de un ítem
/// (#85), valora al paciente (#86) y revaloriza esa valoración (#87) llamando a los manejadores reales, todo
/// contra PostgreSQL real.
/// </summary>
/// <remarks>
/// <c>Menus.Application</c> expone sus internos a este proyecto (<c>InternalsVisibleTo</c>) para llamar a los
/// manejadores reales en vez de repetir su lógica a mano (revisión del #263 y el #277): así la prueba también
/// comprueba sus propias validaciones (que el paciente exista para el servicio, que la asignación exista para la
/// valoración, RS-09 al solicitar la revalorización, RK-11 al registrar su resultado), no solo que las tablas
/// acepten los datos. La solicitud la hace el propio paciente de la valoración revisada, con su identidad del token
/// (#267): los casos en que la pide otro se prueban en <c>Menus.Application.Tests</c>.
/// </remarks>
[Collection("PostgreSQL")]
public sealed class ConsumoYValoracionDePuntaAPuntaTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _s = null!;

    public ConsumoYValoracionDePuntaAPuntaTests(PostgresFixture fixture)
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

    /// <summary>El actor actual, fijo para la prueba: los manejadores de valoración y revalorización lo necesitan.</summary>
    private sealed class ActorFijo(Guid id) : IActorActual
    {
        public Guid Id { get; } = id;
    }

    /// <summary>La identidad del token de un paciente: la solicitud de revalorización la hace él (RS-09, #267).</summary>
    private sealed class PacienteDelToken(Guid pacienteId) : IIdentidadAutenticada
    {
        public Guid UsuarioId { get; } = Guid.NewGuid();

        public Guid? EntidadId { get; } = pacienteId;

        public string Rol => RolesUsuario.Paciente;
    }

    [Fact]
    public async Task Asignacion_consumo_valoracion_y_revalorizacion_de_punta_a_punta()
    {
        var dieta = await _s.DietaAsync();
        var creador = await _s.NutricionistaAsync();
        await _s.AutorizarAsync(creador, dieta);
        var jefe = await _s.NutricionistaAsync(esJefe: true);
        await _s.AutorizarAsync(jefe, dieta);

        var menu = await _s.MenuAsync(dieta, creador);
        var item = await _s.AlimentoAsync();
        await _s.IncluirEnMenuAsync(menu, item);
        await _s.ValidarAsync(menu, jefe, aprobado: true);

        var paciente = await _s.PacienteAsync();
        await _s.InscribirAsync(paciente, dieta);
        await _s.AsignarAsync(menu, paciente);

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        // Consumo (#85): el menú asignado se sirve, con la aceptación del ítem.
        await using (var ctx = _fixture.CrearContexto())
        {
            var handler = new RegistrarServicioHandler(
                new MenuRepository(ctx), new PacienteRepository(ctx), new UnitOfWork(ctx));
            await handler.HandleAsync(
                new RegistrarServicioCommand(menu, paciente, hoy, [new ConsumoDelServicio(item, Aceptacion: true)]),
                CancellationToken.None);
        }

        // Valoración (#86): el nutricionista que creó el menú lo valora (un nutricionista autorizado en la dieta
        // cualquiera podría, según el handler; se usa el creador porque ya está a mano en la prueba).
        Guid valoracionRevisada;
        await using (var ctx = _fixture.CrearContexto())
        {
            var handler = new RegistrarValoracionHandler(
                new MenuRepository(ctx),
                new ValoracionRepository(ctx),
                new UnitOfWork(ctx),
                new ActorFijo(creador),
                new CacheNula());
            var creada = await handler.HandleAsync(
                new RegistrarValoracionCommand(menu, paciente, ResultadoNutricional.Normal.Id), CancellationToken.None);
            valoracionRevisada = creada.Id;
        }

        // Revalorización (#87): el paciente pide revisar esa valoración, y otro nutricionista la ejecuta.
        var ejecutor = await _s.NutricionistaAsync();
        await _s.AutorizarAsync(ejecutor, dieta);

        Guid revalorizacionId;
        await using (var ctx = _fixture.CrearContexto())
        {
            var handler = new SolicitarRevalorizacionHandler(
                new RevalorizacionRepository(ctx),
                new ValoracionRepository(ctx),
                new NutricionistaRepository(ctx),
                new UnitOfWork(ctx),
                new PacienteDelToken(paciente));
            var creada = await handler.HandleAsync(
                new SolicitarRevalorizacionCommand(valoracionRevisada, ejecutor), CancellationToken.None);
            revalorizacionId = creada.Id;
        }

        Guid valoracionGenerada;
        await using (var ctx = _fixture.CrearContexto())
        {
            var handler = new RegistrarResultadoRevalorizacionHandler(
                new RevalorizacionRepository(ctx),
                new ValoracionRepository(ctx),
                new UnitOfWork(ctx),
                new ActorFijo(ejecutor),
                new CacheNula());
            var creada = await handler.HandleAsync(
                new RegistrarResultadoRevalorizacionCommand(revalorizacionId, ResultadoNutricional.Buena.Id),
                CancellationToken.None);
            valoracionGenerada = creada.Id;
        }

        // Comprobación de punta a punta, todo releído desde cero.
        await using var comprobacion = _fixture.CrearContexto();

        var menuFinal = (await new MenuRepository(comprobacion).GetByIdAsync(menu))!;
        var asignacion = Assert.Single(menuFinal.Asignaciones);
        var servicio = Assert.Single(asignacion.Servicios);
        Assert.Equal(hoy, servicio.FechaServicio);
        var consumo = Assert.Single(servicio.Consumos);
        Assert.Equal(item, consumo.ItemId);
        Assert.True(consumo.Aceptacion);

        var valoracionesDelPaciente = await new ValoracionRepository(comprobacion).GetByPacienteAsync(paciente);
        Assert.Equal(2, valoracionesDelPaciente.Count);
        Assert.Contains(valoracionesDelPaciente, v => v.Id == valoracionRevisada);
        Assert.Contains(valoracionesDelPaciente, v => v.Id == valoracionGenerada);

        var revalorizacionFinal = (await new RevalorizacionRepository(comprobacion).GetByIdAsync(revalorizacionId))!;
        Assert.Equal(valoracionRevisada, revalorizacionFinal.ValoracionRevisadaId);
        Assert.Equal(valoracionGenerada, revalorizacionFinal.ValoracionGeneradaId);
    }
}
