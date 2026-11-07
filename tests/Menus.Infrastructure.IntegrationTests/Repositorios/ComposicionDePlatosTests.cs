using System;
using System.Linq;
using System.Threading.Tasks;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Pruebas contra PostgreSQL real de lo que hacen los casos de uso de Platos (#62) con EF: registrar el plato con su
/// composición en una transacción, cambiar la composición de un plato rastreado, y las consultas que agrega el #62.
/// </summary>
[Collection("PostgreSQL")]
public sealed class ComposicionDePlatosTests : IAsyncLifetime
{
    private static readonly Guid GrupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid GrupoFrutas = Guid.Parse("11111111-0000-0000-0000-000000000004");

    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _s = null!;

    public ComposicionDePlatosTests(PostgresFixture fixture)
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

    private static Plato NuevoPlato(string nombre, Guid nutricionistaId) => new(
        Guid.NewGuid(), nombre, GrupoCereales, TipoPreparacion.PlatoFuerte.Id, NivelCalorico.Medio.Id, nutricionistaId);

    [Fact]
    public async Task Registrar_un_plato_con_su_composicion_se_confirma_en_una_transaccion()
    {
        var arroz = await _s.AlimentoConAsync();
        var leche = await _s.AlimentoConAsync();
        var plato = NuevoPlato("Arroz con leche", await _s.NutricionistaAsync());
        plato.AgregarIngrediente(arroz, 200m);
        plato.AgregarIngrediente(leche, 250m);

        await using (var ctx = _fixture.CrearContexto())
        {
            await new PlatoRepository(ctx).AddAsync(plato);
            await new UnitOfWork(ctx).CommitAsync();
        }

        await using var verificacion = _fixture.CrearContexto();
        var guardado = await new PlatoRepository(verificacion).GetConIngredientesAsync(plato.Id);
        Assert.NotNull(guardado);
        Assert.Equal(450m, guardado.Ingredientes.Sum(i => i.Cantidad));
    }

    [Fact]
    public async Task Agregar_y_quitar_ingredientes_de_un_plato_rastreado_se_guardan()
    {
        var lechuga = await _s.AlimentoConAsync();
        var tomate = await _s.AlimentoConAsync();
        var pepino = await _s.AlimentoConAsync();
        var platoId = await _s.PlatoConAsync(null, null, lechuga, tomate);

        await using (var ctx = _fixture.CrearContexto())
        {
            var plato = await new PlatoRepository(ctx).GetConIngredientesAsync(platoId);
            plato!.AgregarIngrediente(pepino, 30m);
            plato.QuitarIngrediente(tomate);
            await new UnitOfWork(ctx).CommitAsync();
        }

        await using var verificacion = _fixture.CrearContexto();
        var guardado = await new PlatoRepository(verificacion).GetConIngredientesAsync(platoId);
        Assert.Equal(
            new[] { lechuga, pepino }.OrderBy(id => id),
            guardado!.Ingredientes.Select(i => i.AlimentoId).OrderBy(id => id));
    }

    [Fact]
    public async Task La_base_rechaza_un_plato_como_ingrediente_aunque_se_omita_la_comprobacion_previa()
    {
        var otroPlato = await _s.PlatoConAsync(null, null, await _s.AlimentoConAsync());
        var plato = NuevoPlato("Arroz con sofrito", await _s.NutricionistaAsync());
        plato.AgregarIngrediente(otroPlato, 10m);

        await using var ctx = _fixture.CrearContexto();
        await new PlatoRepository(ctx).AddAsync(plato);

        // RS-12 es el último recurso: el manejador lo comprueba antes, pero la base lo impide igual.
        await Assert.ThrowsAsync<DbUpdateException>(() => new UnitOfWork(ctx).CommitAsync());
    }

    [Fact]
    public async Task BuscarAsync_de_platos_filtra_pagina_y_no_incluye_alimentos()
    {
        var alimento = await _s.AlimentoConAsync(GrupoCereales, NivelCalorico.Bajo.Id, "Arroz");
        await _s.PlatoConAsync(null, NivelCalorico.Bajo.Id, alimento);
        await _s.PlatoConAsync(null, NivelCalorico.Bajo.Id, alimento);
        await _s.PlatoConAsync(null, NivelCalorico.Alto.Id, alimento);

        await using var ctx = _fixture.CrearContexto();
        var repositorio = new PlatoRepository(ctx);

        var (primera, total) = await repositorio.BuscarAsync(null, null, NivelCalorico.Bajo.Id, 1, 1);
        var (segunda, _) = await repositorio.BuscarAsync(null, null, NivelCalorico.Bajo.Id, 2, 1);

        Assert.Equal(2, total);
        Assert.Single(primera);
        Assert.Single(segunda);
        Assert.NotEqual(primera[0].Id, segunda[0].Id);
        Assert.Empty(primera[0].Ingredientes);
    }

    [Fact]
    public async Task BuscarAsync_de_platos_filtra_por_grupo_y_por_tipo_de_preparacion()
    {
        var alimento = await _s.AlimentoConAsync();
        var entrante = await _s.PlatoConAsync(TipoPreparacion.Entrante.Id, null, alimento);
        await _s.PlatoConAsync(TipoPreparacion.Postre.Id, null, alimento);

        await using var ctx = _fixture.CrearContexto();
        var repositorio = new PlatoRepository(ctx);

        // El sembrador pone los platos en el grupo Cereales.
        var (deCereales, totalCereales) = await repositorio.BuscarAsync(GrupoCereales, null, null, 1, 10);
        var (deFrutas, totalFrutas) = await repositorio.BuscarAsync(GrupoFrutas, null, null, 1, 10);
        var (entrantes, totalEntrantes) = await repositorio.BuscarAsync(GrupoCereales, TipoPreparacion.Entrante.Id, null, 1, 10);

        Assert.Equal(2, totalCereales);
        Assert.Equal(2, deCereales.Count);
        Assert.Equal(0, totalFrutas);
        Assert.Empty(deFrutas);
        Assert.Equal(1, totalEntrantes);
        Assert.Equal(entrante, Assert.Single(entrantes).Id);
    }

    [Fact]
    public async Task GetByIdsAsync_devuelve_solo_los_alimentos_existentes()
    {
        var arroz = await _s.AlimentoConAsync();
        var plato = await _s.PlatoConAsync(null, null, arroz);

        await using var ctx = _fixture.CrearContexto();

        var encontrados = await new AlimentoRepository(ctx).GetByIdsAsync([arroz, plato, Guid.NewGuid()]);

        Assert.Equal(arroz, Assert.Single(encontrados).Id);
    }
}
