using System;
using System.Linq;
using System.Threading.Tasks;
using Menus.Application.GeneracionDeMenus;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Domain.ValueObjects;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Prueba de integración de punta a punta (#83): genera un menú con la estrategia real del #98 sobre ítems
/// sembrados en PostgreSQL, y lo aprueba con el flujo del #99, para comprobar que lo que produce la generación
/// automática efectivamente pasa los triggers que exige la validación, sin usar dobles de ningún tipo.
/// </summary>
/// <remarks>
/// No pasa por <c>GenerarMenuAutomaticamenteHandler</c> ni por <c>ValidarMenuHandler</c>: son <c>internal</c> de
/// <c>Menus.Application</c>, visibles solo para <c>Menus.Application.Tests</c> (donde ya se prueban con
/// repositorios falsos). Aquí se repite la misma orquestación que el primero —filtrar por grupos prohibidos,
/// elegir con la estrategia, armar el <see cref="Menu"/>— contra la base real, igual que
/// <see cref="ValidacionDeMenusTests"/> ya dictamina llamando a <c>Menu.Aprobar</c> directamente en vez de a través
/// del handler.
/// </remarks>
[Collection("PostgreSQL")]
public sealed class GeneracionYValidacionDeMenusTests : IAsyncLifetime
{
    private static readonly Guid GrupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");

    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _s = null!;

    public GeneracionYValidacionDeMenusTests(PostgresFixture fixture)
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

    private async Task<Guid> ItemAsync(Guid tipoPreparacionId, Guid nutricionistaId)
    {
        await using var ctx = _fixture.CrearContexto();
        var item = new Alimento(
            Guid.NewGuid(), $"Item {Guid.NewGuid():N}", GrupoCereales, tipoPreparacionId, NivelCalorico.Medio.Id, nutricionistaId);
        await new ItemAlimentarioRepository(ctx).AddAsync(item);
        await new UnitOfWork(ctx).CommitAsync();
        return item.Id;
    }

    [Fact]
    public async Task Generar_validar_y_aprobar_un_menu_de_punta_a_punta()
    {
        var dieta = await _s.DietaAsync(); // cubre GrupoCereales como requerido (RK-09)
        var creador = await _s.NutricionistaAsync();
        await _s.AutorizarAsync(creador, dieta);
        var jefe = await _s.NutricionistaAsync(esJefe: true);
        await _s.AutorizarAsync(jefe, dieta);

        var entrantes = new Guid[3];
        for (var i = 0; i < entrantes.Length; i++)
        {
            entrantes[i] = await ItemAsync(TipoPreparacion.Entrante.Id, creador);
        }

        var platosFuertes = new Guid[3];
        for (var i = 0; i < platosFuertes.Length; i++)
        {
            platosFuertes[i] = await ItemAsync(TipoPreparacion.PlatoFuerte.Id, creador);
        }

        var cantidad = CantidadTotalAlimentos.Crear(4);
        var distribucion = DistribucionTiposPreparacion.CrearDesdeTuplas(
            [(TipoPreparacion.Entrante.Id, 0.5m), (TipoPreparacion.PlatoFuerte.Id, 0.5m)]);
        var cobertura = CoberturaGruposNutricionales.Crear([GrupoCereales]);

        // Generación (#98): misma orquestación que GenerarMenuAutomaticamenteHandler, contra la base real.
        Guid menuId;
        await using (var ctxGeneracion = _fixture.CrearContexto())
        {
            var gruposProhibidos = await new DietaRepository(ctxGeneracion).GetGruposProhibidosAsync(dieta);
            var todosLosItems = await new ItemAlimentarioRepository(ctxGeneracion).ListAsync();
            var itemsCompatibles = todosLosItems.Where(i => !gruposProhibidos.Contains(i.GrupoNutricionalId)).ToList();

            var seleccionados = new EstrategiaGeneracionAleatoria()
                .SeleccionarItems(itemsCompatibles, distribucion, cobertura, cantidad);

            var menu = new Menu(
                Guid.NewGuid(), dieta, creador, DateTime.UtcNow, cantidad, esAutomatico: true, distribucion, cobertura);
            foreach (var itemId in seleccionados)
            {
                menu.AgregarItem(itemId);
            }

            await new MenuRepository(ctxGeneracion).AddAsync(menu);
            await new UnitOfWork(ctxGeneracion).CommitAsync();
            menuId = menu.Id;
        }

        // Validación y aprobación (#99): mismo flujo que ValidacionDeMenusTests.
        await using (var ctxValidacion = _fixture.CrearContexto())
        {
            var menu = (await new MenuRepository(ctxValidacion).GetByIdAsync(menuId))!;
            menu.Aprobar(jefe, DateTime.UtcNow, "Generado y aprobado automáticamente");
            await new UnitOfWork(ctxValidacion).CommitAsync();
        }

        await using var ctxComprobacion = _fixture.CrearContexto();
        var final = (await new MenuRepository(ctxComprobacion).GetByIdAsync(menuId))!;
        Assert.True(final.EsFinal);
        Assert.Equal(jefe, final.Validacion!.NutricionistaId);
        Assert.Equal(4, final.Items.Count);
        Assert.Equal(4, final.Items.Select(i => i.ItemId).Distinct().Count());
        Assert.Equal(2, final.Items.Count(i => entrantes.Contains(i.ItemId)));
        Assert.Equal(2, final.Items.Count(i => platosFuertes.Contains(i.ItemId)));
    }
}
