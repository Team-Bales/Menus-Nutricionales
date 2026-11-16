using System;
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
/// Prueba de integración de punta a punta (#89): asigna un menú, registra su servicio y el consumo de un ítem
/// (#85), valora al paciente (#86) y revaloriza esa valoración (#87), todo contra PostgreSQL real.
/// </summary>
/// <remarks>
/// Cada paso repite lo que hace su propio manejador (ya probado por separado con repositorios falsos y, el de
/// asignación y validación, también contra esta misma base en <see cref="GeneracionYValidacionDeMenusTests"/>),
/// para comprobar que la cadena completa persiste y se puede volver a leer sin romper ninguna restricción de la
/// base entre un paso y el siguiente.
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
            var menuDb = (await new MenuRepository(ctx).GetByIdAsync(menu))!;
            menuDb.RegistrarServicio(paciente, hoy);
            menuDb.RegistrarConsumo(paciente, hoy, item, aceptacion: true);
            await new UnitOfWork(ctx).CommitAsync();
        }

        // Valoración (#86): el nutricionista valora al paciente por ese menú.
        var valoracionRevisada = await _s.ValorarAsync(menu, paciente, creador);

        // Revalorización (#87): el paciente pide revisar esa valoración, y otro nutricionista la ejecuta.
        var ejecutor = await _s.NutricionistaAsync();
        await _s.AutorizarAsync(ejecutor, dieta);

        Guid revalorizacionId;
        await using (var ctx = _fixture.CrearContexto())
        {
            var revalorizacion = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow, ejecutor, valoracionRevisada);
            await new RevalorizacionRepository(ctx).AddAsync(revalorizacion);
            await new UnitOfWork(ctx).CommitAsync();
            revalorizacionId = revalorizacion.Id;
        }

        Guid valoracionGenerada;
        await using (var ctx = _fixture.CrearContexto())
        {
            var repositorioRevalorizaciones = new RevalorizacionRepository(ctx);
            var repositorioValoraciones = new ValoracionRepository(ctx);

            var revalorizacion = (await repositorioRevalorizaciones.GetByIdAsync(revalorizacionId))!;
            var revisada = (await repositorioValoraciones.GetByIdAsync(revalorizacion.ValoracionRevisadaId))!;

            var generada = new ValoracionNutricional(
                Guid.NewGuid(), revisada.MenuId, revisada.PacienteId, ejecutor, ResultadoNutricional.Buena.Id, DateTime.UtcNow);
            revalorizacion.RegistrarValoracionGenerada(generada.Id);

            await repositorioValoraciones.AddAsync(generada);
            await new UnitOfWork(ctx).CommitAsync();
            valoracionGenerada = generada.Id;
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
