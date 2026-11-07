using System;
using System.Linq;
using System.Threading.Tasks;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence.Repositories;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Pruebas contra PostgreSQL real de las consultas que usan los casos de uso de Alimentos (#61):
/// <see cref="ItemAlimentarioRepository"/> y las que el #61 agrega a <see cref="AlimentoRepository"/>.
/// </summary>
[Collection("PostgreSQL")]
public sealed class ItemAlimentarioRepositoryTests : IAsyncLifetime
{
    private static readonly Guid GrupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid GrupoFrutas = Guid.Parse("11111111-0000-0000-0000-000000000004");

    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _s = null!;

    public ItemAlimentarioRepositoryTests(PostgresFixture fixture)
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

    [Theory]
    [InlineData("Avena")]
    [InlineData("  aVeNa ")]
    public async Task ExisteNombreAsync_encuentra_el_nombre_sin_distinguir_mayusculas_ni_espacios(string buscado)
    {
        await _s.AlimentoConAsync(nombre: "Avena");

        await using var ctx = _fixture.CrearContexto();

        Assert.True(await new ItemAlimentarioRepository(ctx).ExisteNombreAsync(buscado));
        Assert.False(await new ItemAlimentarioRepository(ctx).ExisteNombreAsync("Avena integral"));
    }

    [Fact]
    public async Task ExisteNombreAsync_abarca_tambien_a_los_platos()
    {
        await _s.PlatoConAsync(null, null, await _s.AlimentoConAsync());

        await using var ctx = _fixture.CrearContexto();
        var plato = ctx.Platos.Single();

        Assert.True(await new ItemAlimentarioRepository(ctx).ExisteNombreAsync(plato.Nombre.ToUpperInvariant()));
    }

    [Fact]
    public async Task FiguraEnAlgunMenuAsync_distingue_el_item_incluido_del_que_no()
    {
        var incluido = await _s.AlimentoConAsync();
        var suelto = await _s.AlimentoConAsync();
        var dieta = await _s.DietaAsync();
        var creador = await _s.NutricionistaAsync();
        await _s.AutorizarParaDietaAsync(creador, dieta);
        var menu = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menu, incluido);

        await using var ctx = _fixture.CrearContexto();
        var repositorio = new ItemAlimentarioRepository(ctx);

        Assert.True(await repositorio.FiguraEnAlgunMenuAsync(incluido));
        Assert.False(await repositorio.FiguraEnAlgunMenuAsync(suelto));
    }

    [Fact]
    public async Task FormaParteDeAlgunPlatoAsync_distingue_el_ingrediente_del_que_no()
    {
        var ingrediente = await _s.AlimentoConAsync();
        var suelto = await _s.AlimentoConAsync();
        await _s.PlatoConAsync(null, null, ingrediente);

        await using var ctx = _fixture.CrearContexto();
        var repositorio = new AlimentoRepository(ctx);

        Assert.True(await repositorio.FormaParteDeAlgunPlatoAsync(ingrediente));
        Assert.False(await repositorio.FormaParteDeAlgunPlatoAsync(suelto));
    }

    [Fact]
    public async Task BuscarAsync_filtra_ordena_por_nombre_pagina_y_no_incluye_platos()
    {
        await _s.AlimentoConAsync(GrupoCereales, NivelCalorico.Bajo.Id, "Cebada");
        await _s.AlimentoConAsync(GrupoCereales, NivelCalorico.Bajo.Id, "Arroz");
        await _s.AlimentoConAsync(GrupoCereales, NivelCalorico.Bajo.Id, "Bulgur");
        await _s.AlimentoConAsync(GrupoCereales, NivelCalorico.Alto.Id, "Avena");
        var anon = await _s.AlimentoConAsync(GrupoFrutas, NivelCalorico.Bajo.Id, "Anón");
        await _s.PlatoConAsync(null, NivelCalorico.Bajo.Id, anon); // del grupo Cereales: si se colara, saldría en la página 2

        await using var ctx = _fixture.CrearContexto();
        var repositorio = new AlimentoRepository(ctx);

        var (primera, total) = await repositorio.BuscarAsync(GrupoCereales, null, NivelCalorico.Bajo.Id, 1, 2);
        var (segunda, _) = await repositorio.BuscarAsync(GrupoCereales, null, NivelCalorico.Bajo.Id, 2, 2);

        Assert.Equal(3, total);
        Assert.Equal(["Arroz", "Bulgur"], primera.Select(a => a.Nombre));
        Assert.Equal(["Cebada"], segunda.Select(a => a.Nombre));
    }

    [Fact]
    public async Task BuscarAsync_por_tipo_de_preparacion_y_sin_filtros()
    {
        await _s.ItemAsync("Alimento", tipoId: TipoPreparacion.Postre.Id, nombre: "Guayaba");
        await _s.ItemAsync("Alimento", tipoId: TipoPreparacion.Bebida.Id, nombre: "Té");

        await using var ctx = _fixture.CrearContexto();
        var repositorio = new AlimentoRepository(ctx);

        var (postres, totalPostres) = await repositorio.BuscarAsync(null, TipoPreparacion.Postre.Id, null, 1, 10);
        var (_, totalSinFiltros) = await repositorio.BuscarAsync(null, null, null, 1, 10);

        Assert.Equal(1, totalPostres);
        Assert.Equal("Guayaba", Assert.Single(postres).Nombre);
        Assert.Equal(2, totalSinFiltros);
    }
}
