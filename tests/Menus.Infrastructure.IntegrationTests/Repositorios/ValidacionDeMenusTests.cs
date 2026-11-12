using System;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Pruebas contra PostgreSQL real de lo que hacen los casos de uso del #99 con EF: dictaminar un menú rastreado y
/// vincular un menú de reemplazo. También comprueban que la base respalda las reglas que el caso de uso comprueba
/// antes (RS-03, RK-15).
/// </summary>
[Collection("PostgreSQL")]
public sealed class ValidacionDeMenusTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _s = null!;
    private Guid _dieta;
    private Guid _creador;

    public ValidacionDeMenusTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();
        _conexion = await _fixture.AbrirAsync();
        _s = new Sembrador(_conexion);
        _dieta = await _s.DietaAsync();
        _creador = await _s.NutricionistaAsync();
        await _s.AutorizarAsync(_creador, _dieta);
    }

    public async Task DisposeAsync() => await _conexion.DisposeAsync();

    private async Task<Guid> JefeAutorizadoAsync()
    {
        var jefe = await _s.NutricionistaAsync(esJefe: true);
        await _s.AutorizarAsync(jefe, _dieta);
        return jefe;
    }

    private async Task DictaminarAsync(Guid menuId, Guid revisorId, bool aprobado, string? observaciones = null)
    {
        await using var ctx = _fixture.CrearContexto();
        var menu = (await new MenuRepository(ctx).GetByIdAsync(menuId))!;
        if (aprobado)
        {
            menu.Aprobar(revisorId, DateTime.UtcNow, observaciones);
        }
        else
        {
            menu.Rechazar(revisorId, DateTime.UtcNow, observaciones);
        }

        await new UnitOfWork(ctx).CommitAsync();
    }

    private async Task ReemplazarAsync(Guid menuId, Guid rechazadoId)
    {
        await using var ctx = _fixture.CrearContexto();
        var repositorio = new MenuRepository(ctx);
        var menu = (await repositorio.GetByIdAsync(menuId))!;
        menu.RegistrarComoReemplazo((await repositorio.GetByIdAsync(rechazadoId))!);
        await new UnitOfWork(ctx).CommitAsync();
    }

    [Fact]
    public async Task Aprobar_un_menu_rastreado_persiste_el_dictamen()
    {
        var menuId = await _s.MenuAsync(_dieta, _creador);
        var jefe = await JefeAutorizadoAsync();

        await DictaminarAsync(menuId, jefe, aprobado: true, "Bien balanceado");

        await using var ctx = _fixture.CrearContexto();
        var menu = (await new MenuRepository(ctx).GetByIdAsync(menuId))!;
        Assert.True(menu.EsFinal);
        Assert.Equal(jefe, menu.Validacion!.NutricionistaId);
        Assert.Equal("Bien balanceado", menu.Validacion.Observaciones);
    }

    [Fact]
    public async Task Rechazar_persiste_el_dictamen_negativo_y_el_menu_sigue_existiendo()
    {
        var menuId = await _s.MenuAsync(_dieta, _creador);

        await DictaminarAsync(menuId, await JefeAutorizadoAsync(), aprobado: false, "Faltan frutas");

        await using var ctx = _fixture.CrearContexto();
        var menu = (await new MenuRepository(ctx).GetByIdAsync(menuId))!;
        Assert.True(menu.EsRechazado);
        Assert.Equal("Faltan frutas", menu.Validacion!.Observaciones);
    }

    [Fact]
    public async Task La_base_rechaza_el_dictamen_de_quien_no_es_jefe_RS03()
    {
        var menuId = await _s.MenuAsync(_dieta, _creador);
        var noJefe = await _s.NutricionistaAsync();
        await _s.AutorizarAsync(noJefe, _dieta);

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => DictaminarAsync(menuId, noJefe, aprobado: true));

        var postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.RaiseException, postgres.SqlState);
        Assert.StartsWith("RS-03", postgres.MessageText);
    }

    [Fact]
    public async Task Registrar_un_reemplazo_persiste_el_vinculo_con_el_rechazado()
    {
        var rechazado = await _s.MenuAsync(_dieta, _creador);
        await DictaminarAsync(rechazado, await JefeAutorizadoAsync(), aprobado: false, "Rehacer");
        var nuevo = await _s.MenuAsync(_dieta, _creador);

        await ReemplazarAsync(nuevo, rechazado);

        await using var ctx = _fixture.CrearContexto();
        var menu = (await new MenuRepository(ctx).GetByIdAsync(nuevo))!;
        Assert.Equal(rechazado, menu.ValidacionOrigenId);
        Assert.True(menu.EsPendiente);
    }

    [Fact]
    public async Task La_base_no_deja_que_un_rechazo_origine_dos_menus_RK15()
    {
        var rechazado = await _s.MenuAsync(_dieta, _creador);
        await DictaminarAsync(rechazado, await JefeAutorizadoAsync(), aprobado: false, "Rehacer");
        var primero = await _s.MenuAsync(_dieta, _creador);
        var segundo = await _s.MenuAsync(_dieta, _creador);
        await ReemplazarAsync(primero, rechazado);

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => ReemplazarAsync(segundo, rechazado));

        var postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
    }
}
