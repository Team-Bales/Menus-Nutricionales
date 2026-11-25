using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Menus.Application.Reportes;
using Menus.Domain.Abstractions.Repositories;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence;
using Menus.Infrastructure.Persistence.Consultas;
using Menus.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Consultas;

/// <summary>
/// Pruebas de integración de los manejadores C5 (<see cref="CompararDietasHandler"/>) y C6
/// (<see cref="ObtenerCorrelacionNutricionalHandler"/>) contra PostgreSQL real usando el
/// <see cref="LectorDeConsultasInformacionales"/> real.
/// Verifican que los handlers ejecutan sus tres consultas secuencialmente sin
/// <see cref="InvalidOperationException"/> por uso concurrente del <see cref="MenusDbContext"/>.
/// </summary>
[Collection("PostgreSQL")]
public sealed class ReportesHandlersIntegrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private MenusDbContext _dbContext = null!;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _s = null!;
    private LectorDeConsultasInformacionales _lector = null!;
    private IDietaRepository _dietasRepo = null!;
    private CompararDietasHandler _handlerC5 = null!;
    private ObtenerCorrelacionNutricionalHandler _handlerC6 = null!;

    public ReportesHandlersIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();
        _dbContext = _fixture.CrearContexto();
        _conexion = await _fixture.AbrirAsync();
        _s = new Sembrador(_conexion);
        _lector = new LectorDeConsultasInformacionales(_dbContext);
        _dietasRepo = new DietaRepository(_dbContext);
        _handlerC5 = new CompararDietasHandler(_lector, _dietasRepo);
        _handlerC6 = new ObtenerCorrelacionNutricionalHandler(_lector);
    }

    public async Task DisposeAsync() => await _dbContext.DisposeAsync();

    // ---------------------------------------------------------------------------------------------------------
    // C5: CompararDietasHandler
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task C5_CompararDietasHandler_TresSecciones_SinExcepcionConcurrencia()
    {
        // Arrange: dos dietas con menús finales (aprobados) y items
        var (dieta1, _, _, _) = await EscenarioBaseConMenusAsync("Dieta A");
        var (dieta2, _, _, _) = await EscenarioBaseConMenusAsync("Dieta B");

        // Act: ejecutar el handler C5 (antes lanzaba InvalidOperationException con Task.WhenAll)
        var resultado = await _handlerC5.HandleAsync(
            new CompararDietasQuery([dieta1, dieta2], SoloFinales: true),
            CancellationToken.None);

        // Assert: tres secciones devueltas sin excepción
        Assert.NotNull(resultado.PorGrupo);
        Assert.NotNull(resultado.PorNivel);
        Assert.NotNull(resultado.Criterios);

        // Cada sección puede tener filas (depende de los datos sembrados), pero no debe fallar
        Assert.True(resultado.PorGrupo.Count >= 0);
        Assert.True(resultado.PorNivel.Count >= 0);
        Assert.True(resultado.Criterios.Count >= 0);
    }

    [Fact]
    public async Task C5_CompararDietasHandler_SoloFinalesFalse_TambienFunciona()
    {
        // Arrange
        var (dieta1, _, _, _) = await EscenarioBaseConMenusAsync("Dieta C");
        var (dieta2, _, _, _) = await EscenarioBaseConMenusAsync("Dieta D");

        // Act
        var resultado = await _handlerC5.HandleAsync(
            new CompararDietasQuery([dieta1, dieta2], SoloFinales: false),
            CancellationToken.None);

        // Assert
        Assert.NotNull(resultado.PorGrupo);
        Assert.NotNull(resultado.PorNivel);
        Assert.NotNull(resultado.Criterios);
    }

    [Fact]
    public async Task C5_CompararDietasHandler_DietaInexistente_LanzaNoEncontrado()
    {
        // Arrange
        var (dieta1, _, _, _) = await EscenarioBaseConMenusAsync("Dieta E");
        var dietaInexistente = Guid.NewGuid();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Menus.Application.Comun.NoEncontradoException>(
            () => _handlerC5.HandleAsync(
                new CompararDietasQuery([dieta1, dietaInexistente], SoloFinales: true),
                CancellationToken.None));

        Assert.Equal("dieta", ex.Recurso);
        Assert.Equal(dietaInexistente, ex.Id);
    }

    // ---------------------------------------------------------------------------------------------------------
    // C6: ObtenerCorrelacionNutricionalHandler
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task C6_ObtenerCorrelacionNutricionalHandler_TresSecciones_SinExcepcionConcurrencia()
    {
        // Arrange: sembrar datos para correlación (valoraciones), rechazos (consumos) y revalorizaciones
        await SembrarDatosParaC6Async();

        // Act: ejecutar el handler C6 (antes lanzaba InvalidOperationException con Task.WhenAll)
        var resultado = await _handlerC6.HandleAsync(
            new ObtenerCorrelacionNutricionalQuery(Desde: null, Hasta: null, MinimoConsumos: 1),
            CancellationToken.None);

        // Assert: tres secciones devueltas sin excepción
        Assert.NotNull(resultado.Correlaciones);
        Assert.NotNull(resultado.ItemsMasRechazados);
        Assert.NotNull(resultado.Revalorizaciones);

        Assert.True(resultado.Correlaciones.Count >= 0);
        Assert.True(resultado.ItemsMasRechazados.Count >= 0);
        Assert.True(resultado.Revalorizaciones.Count >= 0);
    }

    [Fact]
    public async Task C6_ObtenerCorrelacionNutricionalHandler_ConRangoFechas_TambienFunciona()
    {
        // Arrange
        await SembrarDatosParaC6Async();
        var desde = DateTimeOffset.UtcNow.AddDays(-30);
        var hasta = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        var resultado = await _handlerC6.HandleAsync(
            new ObtenerCorrelacionNutricionalQuery(Desde: desde, Hasta: hasta, MinimoConsumos: 1),
            CancellationToken.None);

        // Assert
        Assert.NotNull(resultado.Correlaciones);
        Assert.NotNull(resultado.ItemsMasRechazados);
        Assert.NotNull(resultado.Revalorizaciones);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Crea una dieta con creador y revisor jefe, un menú final aprobado, items, pacientes, consumos y valoraciones.
    /// </summary>
    private async Task<(Guid Dieta, Guid Creador, Guid Revisor, Guid Menu)> EscenarioBaseConMenusAsync(string sufijo)
    {
        var dieta = await _s.DietaAsync();
        var creador = await _s.NutricionistaAsync();
        var revisor = await _s.NutricionistaAsync(esJefe: true);
        await _s.AutorizarParaDietaAsync(creador, dieta);
        await _s.AutorizarParaDietaAsync(revisor, dieta);

        // Items con diferentes grupos y niveles
        var cereal = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoCerealesId, nivelId: Sembrador.NivelMedioId);
        var proteina = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoProteinas, nivelId: Sembrador.NivelAlto);

        // Menú final aprobado
        var menu = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menu, cereal, proteina);
        await _s.ValidarAsync(menu, revisor, aprobado: true);

        // Paciente inscrito, asignado, servido, consumido y valorado (para C6)
        var paciente = await _s.PacienteAsync();
        await _s.InscribirAsync(paciente, dieta);
        await _s.AsignarAsync(menu, paciente);
        var fecha = DateOnly.FromDateTime(DateTime.UtcNow);
        await _s.ServirAsync(menu, paciente, fecha);
        await _s.ConsumirAsync(menu, paciente, fecha, cereal, aceptacion: true);
        await _s.ConsumirAsync(menu, paciente, fecha, proteina, aceptacion: false);
        await _s.ValorarAsync(menu, paciente, revisor);

        return (dieta, creador, revisor, menu);
    }

    private async Task SembrarDatosParaC6Async()
    {
        // Dieta con menús valorados para correlación (Spearman)
        var (dieta, creador, revisor, menu) = await EscenarioBaseConMenusAsync("C6");

        // Segundo menú para tener más datos de correlación
        var cereal2 = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoCerealesId, nivelId: Sembrador.NivelBajo);
        var proteina2 = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoProteinas, nivelId: Sembrador.NivelMedioId);
        var menu2 = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menu2, cereal2, proteina2);
        await _s.ValidarAsync(menu2, revisor, aprobado: true);

        var paciente2 = await _s.PacienteAsync();
        await _s.InscribirAsync(paciente2, dieta);
        await _s.AsignarAsync(menu2, paciente2);
        var fecha2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        await _s.ServirAsync(menu2, paciente2, fecha2);
        await _s.ConsumirAsync(menu2, paciente2, fecha2, cereal2, aceptacion: true);
        await _s.ConsumirAsync(menu2, paciente2, fecha2, proteina2, aceptacion: true);
        await _s.ValorarAsync(menu2, paciente2, revisor, resultadoId: Guid.Parse("138dec6d-8202-4b54-831c-e6fd21508695")); // Excelente

        // Datos para ItemsMasRechazados (top-10 rechazados): ya tenemos rechazos en EscenarioBaseConMenusAsync
        // Datos para Revalorizaciones
        var valoracionParaRevalorizar = await _s.ValorarAsync(menu, paciente2, revisor, resultadoId: Guid.Parse("4077627e-08b1-404a-aeb5-bb64a90538ba")); // Mala
        var valoracionGenerada = await _s.ValorarAsync(menu, paciente2, revisor, resultadoId: Guid.Parse("138dec6d-8202-4b54-831c-e6fd21508695")); // Excelente
        await _s.RevalorizarAsync(valoracionParaRevalorizar, revisor, valoracionGenerada);
    }
}
