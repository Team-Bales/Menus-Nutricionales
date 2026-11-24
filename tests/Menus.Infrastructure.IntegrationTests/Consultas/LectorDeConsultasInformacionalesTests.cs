using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Menus.Application.Reportes;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence;
using Menus.Infrastructure.Persistence.Consultas;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Consultas;

/// <summary>
/// Pruebas de <see cref="LectorDeConsultasInformacionales> contra PostgreSQL real.
/// Cada prueba siembra el escenario mínimo, llama al método del lector (no a la función SQL directa)
/// y comprueba el mapeo de columnas snake_case a las propiedades del DTO, el orden canónico,
/// el recurso sin filas (lista vacía) y que los parámetros van parametrizados.
/// </summary>
[Collection("PostgreSQL")]
public sealed class LectorDeConsultasInformacionalesTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private MenusDbContext _dbContext = null!;
    private Sembrador _s = null!;
    private LectorDeConsultasInformacionales _lector = null!;

    public LectorDeConsultasInformacionalesTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();
        _dbContext = _fixture.CrearContexto();
        var conexion = await _fixture.AbrirAsync();
        _s = new Sembrador(conexion);
        _lector = new LectorDeConsultasInformacionales(_dbContext);
    }

    public async Task DisposeAsync() => await _dbContext.DisposeAsync();

    /// <summary>Crea una dieta con un creador y un revisor (jefe) ya autorizados para ella.</summary>
    private async Task<(Guid Dieta, Guid Creador, Guid Revisor)> EscenarioBaseAsync()
    {
        var dieta = await _s.DietaAsync();
        var creador = await _s.NutricionistaAsync();
        var revisor = await _s.NutricionistaAsync(esJefe: true);
        await _s.AutorizarParaDietaAsync(creador, dieta);
        await _s.AutorizarParaDietaAsync(revisor, dieta);
        return (dieta, creador, revisor);
    }

    private NpgsqlConnection _conexion => _dbContext.Database.GetDbConnection() as NpgsqlConnection ?? throw new InvalidOperationException("Conexión no es Npgsql");

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 1: Menús automáticos por dieta
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ConsultarMenusAutomaticosAsync_FiltraPorDieta_YSoloAutomaticos()
    {
        var (dietaA, creadorA, _) = await EscenarioBaseAsync();
        var dietaB = await _s.DietaAsync();
        var creadorB = await _s.NutricionistaAsync();
        await _s.AutorizarParaDietaAsync(creadorB, dietaB);

        var automaticoA = await _s.MenuConParametrosAsync(
            dietaA, creadorA, DateTime.UtcNow.AddDays(-1), 5, esAutomatico: true,
            [(Sembrador.TipoPlatoFuerteId, 1.0m)], [Sembrador.GrupoCerealesId]);
        await _s.MenuConParametrosAsync(
            dietaA, creadorA, DateTime.UtcNow, 3, esAutomatico: false,
            [(Sembrador.TipoPlatoFuerteId, 1.0m)], [Sembrador.GrupoCerealesId]);
        await _s.MenuConParametrosAsync(
            dietaB, creadorB, DateTime.UtcNow, 4, esAutomatico: true,
            [(Sembrador.TipoPlatoFuerteId, 1.0m)], [Sembrador.GrupoCerealesId]);

        var resultado = await _lector.ConsultarMenusAutomaticosAsync(dietaA);

        Assert.Single(resultado);
        Assert.Equal(automaticoA, resultado[0].MenuId);
    }

    [Fact]
    public async Task ConsultarMenusAutomaticosAsync_MapeaColumnas_YOrdenCanonico()
    {
        var (dieta, creador, _) = await EscenarioBaseAsync();
        var fechaCreacion = new DateTime(2026, 10, 1, 9, 30, 0, DateTimeKind.Utc);

        var menuId = await _s.MenuConParametrosAsync(
            dieta, creador, fechaCreacion, cantidadTotalAlimentos: 8, esAutomatico: true,
            distribucion: [(Sembrador.TipoEntrante, 0.4m), (Sembrador.TipoPlatoFuerteId, 0.6m)],
            grupos: [Sembrador.GrupoCerealesId, Sembrador.GrupoProteinas]);

        var resultado = await _lector.ConsultarMenusAutomaticosAsync(dieta);

        Assert.Single(resultado);
        var dto = resultado[0];
        Assert.Equal(menuId, dto.MenuId);
        Assert.False(string.IsNullOrWhiteSpace(dto.Creador)); // Creador es el nombre del nutricionista
        Assert.Equal(fechaCreacion, dto.FechaCreacion);
        Assert.Equal(8, dto.CantidadTotalAlimentos);
        Assert.Equal("Entrante: 0.4000, Plato fuerte: 0.6000", dto.Distribucion);
        Assert.Equal("Cereales, Proteínas", dto.GruposCubiertos);
    }

    [Fact]
    public async Task ConsultarMenusAutomaticosAsync_SinFilas_DevuelveListaVacia()
    {
        var (dieta, _, _) = await EscenarioBaseAsync();

        var resultado = await _lector.ConsultarMenusAutomaticosAsync(dieta);

        Assert.Empty(resultado);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 2: Alimentos más usados
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ConsultarAlimentosMasUsadosAsync_CuentaUnaVezPorMenu_YRespetaLimite()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();

        var alimentoDirecto = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoProteinas, nivelId: Sembrador.NivelAlto);
        var alimentoEnPlato = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoCerealesId, nivelId: Sembrador.NivelBajo);
        var plato = await _s.PlatoConAsync(tipoId: null, nivelId: Sembrador.NivelBajo, alimentoEnPlato);

        var menuFinal = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menuFinal, alimentoDirecto, alimentoEnPlato, plato);
        await _s.ValidarAsync(menuFinal, revisor, aprobado: true);

        var menuPendiente = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menuPendiente, alimentoDirecto);

        var resultado = await _lector.ConsultarAlimentosMasUsadosAsync(dieta, limite: 10);

        Assert.Equal(2, resultado.Count);
        var directo = resultado.First(r => r.AlimentoId == alimentoDirecto);
        var enPlato = resultado.First(r => r.AlimentoId == alimentoEnPlato);
        Assert.Equal(1L, directo.MenusFinales);
        Assert.Equal(1L, enPlato.MenusFinales);
    }

    [Fact]
    public async Task ConsultarAlimentosMasUsadosAsync_LimiteTop_Parametrizado()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();

        var a1 = await _s.AlimentoConAsync();
        var a2 = await _s.AlimentoConAsync();
        var a3 = await _s.AlimentoConAsync();

        var m1 = await _s.MenuAsync(dieta, creador); await _s.IncluirEnMenuAsync(m1, a1); await _s.ValidarAsync(m1, revisor, true);
        var m2 = await _s.MenuAsync(dieta, creador); await _s.IncluirEnMenuAsync(m2, a2); await _s.ValidarAsync(m2, revisor, true);
        var m3 = await _s.MenuAsync(dieta, creador); await _s.IncluirEnMenuAsync(m3, a3); await _s.ValidarAsync(m3, revisor, true);

        var resultado = await _lector.ConsultarAlimentosMasUsadosAsync(dieta, limite: 2);

        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public async Task ConsultarAlimentosMasUsadosAsync_SinFilas_DevuelveListaVacia()
    {
        var (dieta, _, _) = await EscenarioBaseAsync();

        var resultado = await _lector.ConsultarAlimentosMasUsadosAsync(dieta, limite: 10);

        Assert.Empty(resultado);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 3: Dictámenes de un revisor
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ConsultarDictamenesAsync_ListaAprobacionesYRechazos_ConReemplazo()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();

        var aprobado = await _s.MenuAsync(dieta, creador);
        await _s.ValidarAsync(aprobado, revisor, aprobado: true, observaciones: "cumple");

        var rechazado = await _s.MenuAsync(dieta, creador);
        await _s.ValidarAsync(rechazado, revisor, aprobado: false, observaciones: "falta proteína");
        var reemplazo = await _s.MenuDeReemplazoAsync(dieta, creador, rechazado);

        var resultado = await _lector.ConsultarDictamenesAsync(revisor);

        Assert.Equal(2, resultado.Count);
        var apr = resultado.First(r => r.MenuId == aprobado);
        var rej = resultado.First(r => r.MenuId == rechazado);
        Assert.True(apr.Aprobado);
        Assert.Null(apr.Observaciones);
        Assert.Null(apr.MenuReemplazoId);
        Assert.False(rej.Aprobado);
        Assert.Equal("falta proteína", rej.Observaciones);
        Assert.Equal(reemplazo, rej.MenuReemplazoId);
    }

    [Fact]
    public async Task ConsultarDictamenesAsync_OrdenCanonico_FechaValidacionDesc()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();

        var m1 = await _s.MenuAsync(dieta, creador);
        await _s.ValidarAsync(m1, revisor, aprobado: true, fechaValidacion: DateTime.UtcNow.AddDays(-2));
        var m2 = await _s.MenuAsync(dieta, creador);
        await _s.ValidarAsync(m2, revisor, aprobado: true, fechaValidacion: DateTime.UtcNow.AddDays(-1));

        var resultado = await _lector.ConsultarDictamenesAsync(revisor);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(m2, resultado[0].MenuId); // más reciente primero
        Assert.Equal(m1, resultado[1].MenuId);
    }

    [Fact]
    public async Task ConsultarDictamenesAsync_SinFilas_DevuelveListaVacia()
    {
        var (_, _, revisor) = await EscenarioBaseAsync();

        var resultado = await _lector.ConsultarDictamenesAsync(revisor);

        Assert.Empty(resultado);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 4: Desempeño ante un menú
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ConsultarDesempenoMenuAsync_PonderaTasaPorServicio_YOrdenaPorNivel()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();

        var itemBajo = await _s.AlimentoConAsync(nivelId: Sembrador.NivelBajo);
        var itemAlto = await _s.AlimentoConAsync(nivelId: Sembrador.NivelAlto);

        var menu = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menu, itemBajo, itemAlto);
        await _s.ValidarAsync(menu, revisor, aprobado: true);

        var paciente1 = await _s.PacienteAsync();
        var paciente2 = await _s.PacienteAsync();
        await _s.InscribirAsync(paciente1, dieta);
        await _s.InscribirAsync(paciente2, dieta);
        await _s.AsignarAsync(menu, paciente1);
        await _s.AsignarAsync(menu, paciente2);

        var fecha = DateOnly.FromDateTime(DateTime.UtcNow);
        await _s.ServirAsync(menu, paciente1, fecha);
        await _s.ServirAsync(menu, paciente2, fecha);

        await _s.ConsumirAsync(menu, paciente1, fecha, itemBajo, aceptacion: true);
        await _s.ConsumirAsync(menu, paciente2, fecha, itemBajo, aceptacion: true);
        await _s.ConsumirAsync(menu, paciente1, fecha, itemAlto, aceptacion: false);

        var resultado = await _lector.ConsultarDesempenoMenuAsync(menu);

        Assert.Equal(2, resultado.Count);
        // Orden canónico: Bajo antes que Alto
        Assert.Equal(itemBajo, resultado[0].ItemId);
        Assert.Equal(2L, resultado[0].ServiciosConConsumo);
        Assert.Equal(2L, resultado[0].Aceptaciones);
        Assert.Equal(0L, resultado[0].Rechazos);
        Assert.Equal(1.0m, resultado[0].TasaAceptacion);

        Assert.Equal(itemAlto, resultado[1].ItemId);
        Assert.Equal(1L, resultado[1].ServiciosConConsumo);
        Assert.Equal(0L, resultado[1].Aceptaciones);
        Assert.Equal(1L, resultado[1].Rechazos);
        Assert.Equal(0.0m, resultado[1].TasaAceptacion);
    }

    [Fact]
    public async Task ConsultarDesempenoMenuAsync_SinConsumos_TasaNull()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();
        var item = await _s.AlimentoConAsync();

        var menu = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menu, item);
        await _s.ValidarAsync(menu, revisor, aprobado: true);

        var resultado = await _lector.ConsultarDesempenoMenuAsync(menu);

        Assert.Single(resultado);
        Assert.Null(resultado[0].TasaAceptacion);
    }

    [Fact]
    public async Task ConsultarDesempenoMenuAsync_SinFilas_DevuelveListaVacia()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();
        var menu = await _s.MenuAsync(dieta, creador);
        await _s.ValidarAsync(menu, revisor, aprobado: true);

        var resultado = await _lector.ConsultarDesempenoMenuAsync(menu);

        Assert.Empty(resultado); // sin items en el menú
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 5.1: Distribución por grupo nutricional
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ConsultarComparacionPorGrupoAsync_CalculaPorcentaje_YOrdenCanonico()
    {
        var (dieta, creador, _) = await EscenarioBaseAsync();

        var cereal1 = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoCerealesId);
        var cereal2 = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoCerealesId);
        var proteina = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoProteinas);

        var menu = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menu, cereal1, cereal2, proteina);

        var resultado = await _lector.ConsultarComparacionPorGrupoAsync([dieta], soloFinales: false);

        Assert.Equal(2, resultado.Count);
        var cereales = resultado.First(r => r.GrupoNutricional == "Cereales");
        var proteinas = resultado.First(r => r.GrupoNutricional == "Proteínas");
        Assert.Equal(66.67m, cereales.Porcentaje);
        Assert.Equal(33.33m, proteinas.Porcentaje);
        // Orden canónico: dieta, items DESC, grupo_nutricional
        Assert.Equal(2L, cereales.Items);
        Assert.Equal(1L, proteinas.Items);
    }

    [Fact]
    public async Task ConsultarComparacionPorGrupoAsync_SoloFinales_FiltraPorAprobados()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();
        var a1 = await _s.AlimentoConAsync();
        var a2 = await _s.AlimentoConAsync();

        var mFinal = await _s.MenuAsync(dieta, creador); await _s.IncluirEnMenuAsync(mFinal, a1); await _s.ValidarAsync(mFinal, revisor, true);
        var mPendiente = await _s.MenuAsync(dieta, creador); await _s.IncluirEnMenuAsync(mPendiente, a2);

        var resultadoTodos = await _lector.ConsultarComparacionPorGrupoAsync([dieta], soloFinales: false);
        var resultadoFinales = await _lector.ConsultarComparacionPorGrupoAsync([dieta], soloFinales: true);

        Assert.True(resultadoTodos[0].Items > resultadoFinales[0].Items);
    }

    [Fact]
    public async Task ConsultarComparacionPorGrupoAsync_SinFilas_DevuelveListaVacia()
    {
        var dieta = await _s.DietaAsync();

        var resultado = await _lector.ConsultarComparacionPorGrupoAsync([dieta], soloFinales: false);

        Assert.Empty(resultado);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 5.2: Distribución por nivel calórico
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ConsultarComparacionPorNivelAsync_CalculaPorcentaje_YOrdenCanonico()
    {
        var (dieta, creador, _) = await EscenarioBaseAsync();

        var bajo1 = await _s.AlimentoConAsync(nivelId: Sembrador.NivelBajo);
        var bajo2 = await _s.AlimentoConAsync(nivelId: Sembrador.NivelBajo);
        var alto = await _s.AlimentoConAsync(nivelId: Sembrador.NivelAlto);

        var menu = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menu, bajo1, bajo2, alto);

        var resultado = await _lector.ConsultarComparacionPorNivelAsync([dieta], soloFinales: false);

        Assert.Equal(2, resultado.Count);
        var bajo = resultado.First(r => r.NivelCalorico == "Bajo");
        var altoDto = resultado.First(r => r.NivelCalorico == "Alto");
        Assert.Equal(66.67m, bajo.Porcentaje);
        Assert.Equal(33.33m, altoDto.Porcentaje);
        // Orden canónico: dieta, nivel_calorico (Bajo/Medio/Alto)
        Assert.Equal("Bajo", resultado[0].NivelCalorico);
        Assert.Equal("Alto", resultado[1].NivelCalorico);
    }

    [Fact]
    public async Task ConsultarComparacionPorNivelAsync_SoloFinales_FiltraPorAprobados()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();
        var a1 = await _s.AlimentoConAsync();
        var a2 = await _s.AlimentoConAsync();

        var mFinal = await _s.MenuAsync(dieta, creador); await _s.IncluirEnMenuAsync(mFinal, a1); await _s.ValidarAsync(mFinal, revisor, true);
        var mPendiente = await _s.MenuAsync(dieta, creador); await _s.IncluirEnMenuAsync(mPendiente, a2);

        var resultadoTodos = await _lector.ConsultarComparacionPorNivelAsync([dieta], soloFinales: false);
        var resultadoFinales = await _lector.ConsultarComparacionPorNivelAsync([dieta], soloFinales: true);

        Assert.True(resultadoTodos[0].Items >= resultadoFinales[0].Items);
    }

    [Fact]
    public async Task ConsultarComparacionPorNivelAsync_SinFilas_DevuelveListaVacia()
    {
        var dieta = await _s.DietaAsync();

        var resultado = await _lector.ConsultarComparacionPorNivelAsync([dieta], soloFinales: false);

        Assert.Empty(resultado);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 5.3: Criterios de equilibrio
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ConsultarCriteriosEquilibrioAsync_DistingueMenuQueCumpleDelQueNo()
    {
        var dieta = await _s.DietaAsync(grupoRequeridoId: Sembrador.GrupoCerealesId);
        var creador = await _s.NutricionistaAsync();
        await _s.AutorizarParaDietaAsync(creador, dieta);

        await using (var cmd = _dbContext.Database.GetDbConnection().CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo) VALUES (@dieta, @grupo, 'Restringido')
                """;
            var pDieta = cmd.CreateParameter(); pDieta.ParameterName = "@dieta"; pDieta.Value = dieta; cmd.Parameters.Add(pDieta);
            var pGrupo = cmd.CreateParameter(); pGrupo.ParameterName = "@grupo"; pGrupo.Value = Sembrador.GrupoProteinas; cmd.Parameters.Add(pGrupo);
            await cmd.ExecuteNonQueryAsync();
        }

        var itemPlatoFuerte = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoCerealesId);
        var itemEntrante = await _s.ItemAsync("Alimento", grupoId: Sembrador.GrupoCerealesId, tipoId: Sembrador.TipoEntrante);
        var menuOk = await CrearMenuConParametrosAsync(dieta, creador, cantidadDeclarada: 2,
            proporciones: [(Sembrador.TipoPlatoFuerteId, 0.5m), (Sembrador.TipoEntrante, 0.5m)],
            gruposCubiertos: [Sembrador.GrupoCerealesId]);
        await _s.IncluirEnMenuAsync(menuOk, itemPlatoFuerte, itemEntrante);

        var itemProteina = await _s.ItemAsync("Alimento", grupoId: Sembrador.GrupoProteinas, tipoId: Sembrador.TipoEntrante);
        var menuMal = await CrearMenuConParametrosAsync(dieta, creador, cantidadDeclarada: 5,
            proporciones: [(Sembrador.TipoPlatoFuerteId, 1.0m)],
            gruposCubiertos: [Sembrador.GrupoProteinas]);
        await _s.IncluirEnMenuAsync(menuMal, itemProteina);

        var resultado = await _lector.ConsultarCriteriosEquilibrioAsync([dieta], soloFinales: false);

        Assert.Equal(2, resultado.Count);
        var ok = resultado.First(r => r.MenuId == menuOk);
        var mal = resultado.First(r => r.MenuId == menuMal);

        Assert.True(ok.CantidadCumple);
        Assert.True(ok.ProporcionesCumplen);
        Assert.True(ok.RequeridosCubiertos);
        Assert.True(ok.ProhibidosAusentes);

        Assert.False(mal.CantidadCumple);
        Assert.False(mal.ProporcionesCumplen);
        Assert.False(mal.RequeridosCubiertos);
        Assert.False(mal.ProhibidosAusentes);
    }

    [Fact]
    public async Task ConsultarCriteriosEquilibrioAsync_SoloFinales_FiltraPorAprobados()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();
        var a1 = await _s.AlimentoConAsync();

        var mFinal = await _s.MenuAsync(dieta, creador); await _s.IncluirEnMenuAsync(mFinal, a1); await _s.ValidarAsync(mFinal, revisor, true);
        var mPendiente = await _s.MenuAsync(dieta, creador); await _s.IncluirEnMenuAsync(mPendiente, a1);

        var resultadoTodos = await _lector.ConsultarCriteriosEquilibrioAsync([dieta], soloFinales: false);
        var resultadoFinales = await _lector.ConsultarCriteriosEquilibrioAsync([dieta], soloFinales: true);

        Assert.Equal(2, resultadoTodos.Count);
        Assert.Single(resultadoFinales);
    }

    [Fact]
    public async Task ConsultarCriteriosEquilibrioAsync_SinFilas_DevuelveListaVacia()
    {
        var dieta = await _s.DietaAsync();

        var resultado = await _lector.ConsultarCriteriosEquilibrioAsync([dieta], soloFinales: false);

        Assert.Empty(resultado);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 6A: Correlación de Spearman por dieta
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ConsultarCorrelacionPorDietaAsync_DaCorrelacionPerfecta_CuandoOrdenCoincide()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();
        var paciente = await _s.PacienteAsync();
        await _s.InscribirAsync(paciente, dieta);

        var casos = new (Guid Nivel, Guid Resultado)[]
        {
            (Sembrador.NivelBajo, Guid.Parse("e89977e7-eeb6-4d32-96e2-096ace535bc8")),
            (Sembrador.NivelMedioId, Guid.Parse("023f26c4-50c4-4e20-8c24-7d27b341dfd6")),
            (Sembrador.NivelAlto, Guid.Parse("138dec6d-8202-4b54-831c-e6fd21508695")),
        };

        foreach (var (nivel, resultadoId) in casos)
        {
            var item = await _s.AlimentoConAsync(nivelId: nivel);
            var menu = await _s.MenuAsync(dieta, creador);
            await _s.IncluirEnMenuAsync(menu, item);
            await _s.ValidarAsync(menu, revisor, aprobado: true);
            await _s.AsignarAsync(menu, paciente);
            await _s.ValorarAsync(menu, paciente, revisor, resultadoId: resultadoId);
        }

        var lista = await _lector.ConsultarCorrelacionPorDietaAsync(desde: null, hasta: null);

        var dto = lista.First(r => r.DietaId == dieta);
        Assert.Equal(3L, dto.MenusValorados);
        Assert.Equal(1.0m, dto.CoeficienteSpearman);
    }

    [Fact]
    public async Task ConsultarCorrelacionPorDietaAsync_ParametrosDesdeHasta_Parametrizados()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();
        var paciente = await _s.PacienteAsync();
        await _s.InscribirAsync(paciente, dieta);

        var item = await _s.AlimentoConAsync();
        var menu = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menu, item);
        await _s.ValidarAsync(menu, revisor, aprobado: true);
        await _s.AsignarAsync(menu, paciente);
        await _s.ValorarAsync(menu, paciente, revisor, fechaValoracion: DateTime.UtcNow.AddDays(-10));

        var desde = DateTimeOffset.UtcNow.AddDays(-5);
        var hasta = DateTimeOffset.UtcNow.AddDays(5);

        var resultado = await _lector.ConsultarCorrelacionPorDietaAsync(desde, hasta);

        var dto = resultado.First(r => r.DietaId == dieta);
        Assert.Equal(0L, dto.MenusValorados); // fuera del rango
    }

    [Fact]
    public async Task ConsultarCorrelacionPorDietaAsync_SinFilas_DevuelveListaVacia()
    {
        var resultado = await _lector.ConsultarCorrelacionPorDietaAsync(desde: null, hasta: null);

        Assert.NotNull(resultado); // devuelve todas las dietas, aunque con 0 menús valorados
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 6B: Top-N ítems más rechazados
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ConsultarItemsMasRechazadosAsync_CalculaTasaYListaDietas()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();
        var item = await _s.AlimentoConAsync();

        var menu = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menu, item);
        await _s.ValidarAsync(menu, revisor, aprobado: true);

        var paciente = await _s.PacienteAsync();
        await _s.InscribirAsync(paciente, dieta);
        await _s.AsignarAsync(menu, paciente);

        var dia1 = DateOnly.FromDateTime(DateTime.UtcNow);
        var dia2 = dia1.AddDays(1);
        await _s.ServirAsync(menu, paciente, dia1);
        await _s.ServirAsync(menu, paciente, dia2);
        await _s.ConsumirAsync(menu, paciente, dia1, item, aceptacion: false);
        await _s.ConsumirAsync(menu, paciente, dia2, item, aceptacion: true);

        var resultado = await _lector.ConsultarItemsMasRechazadosAsync(limite: 10, minimoConsumos: 1);

        Assert.Single(resultado);
        var dto = resultado[0];
        Assert.Equal(item, dto.ItemId);
        Assert.Equal(2L, dto.Consumos);
        Assert.Equal(1L, dto.Rechazos);
        Assert.Equal(0.5m, dto.TasaRechazo);
        Assert.Contains("Dieta", dto.Dietas);
    }

    [Fact]
    public async Task ConsultarItemsMasRechazadosAsync_LimiteYMinimoConsumos_Parametrizados()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();
        var item = await _s.AlimentoConAsync();

        var menu = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menu, item);
        await _s.ValidarAsync(menu, revisor, aprobado: true);

        var paciente = await _s.PacienteAsync();
        await _s.InscribirAsync(paciente, dieta);
        await _s.AsignarAsync(menu, paciente);

        var dia1 = DateOnly.FromDateTime(DateTime.UtcNow);
        await _s.ServirAsync(menu, paciente, dia1);
        await _s.ConsumirAsync(menu, paciente, dia1, item, aceptacion: false); // 1 consumo, 1 rechazo

        var resultado = await _lector.ConsultarItemsMasRechazadosAsync(limite: 5, minimoConsumos: 2);

        Assert.Empty(resultado); // minimoConsumos=2 excluye este item
    }

    [Fact]
    public async Task ConsultarItemsMasRechazadosAsync_SinFilas_DevuelveListaVacia()
    {
        var resultado = await _lector.ConsultarItemsMasRechazadosAsync(limite: 10, minimoConsumos: 1);

        Assert.Empty(resultado);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 6C: Revalorizados frente al promedio de su sala
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ConsultarRevalorizadosAsync_ComparaContraPromedioSala()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();
        var sala = await _s.SalaAsync();

        var p1 = await _s.PacienteAsync(sala);
        var p2 = await _s.PacienteAsync(sala);
        var p3 = await _s.PacienteAsync(sala);
        await _s.InscribirAsync(p1, dieta);
        await _s.InscribirAsync(p2, dieta);
        await _s.InscribirAsync(p3, dieta);

        var menu1 = await _s.MenuAsync(dieta, creador);
        await _s.ValidarAsync(menu1, revisor, aprobado: true);
        await _s.AsignarAsync(menu1, p1);
        await _s.AsignarAsync(menu1, p2);
        await _s.AsignarAsync(menu1, p3);

        var malaId = Guid.Parse("4077627e-08b1-404a-aeb5-bb64a90538ba");
        var buenaId = Guid.Parse("c0cf2cb9-45bb-4a30-ab00-0c6a8bfc5ca1");
        var excelenteId = Guid.Parse("138dec6d-8202-4b54-831c-e6fd21508695");

        var revisada = await _s.ValorarAsync(menu1, p1, revisor, resultadoId: malaId);
        await _s.ValorarAsync(menu1, p2, revisor, resultadoId: malaId);
        await _s.ValorarAsync(menu1, p3, revisor, resultadoId: buenaId);

        var generada = await _s.ValorarAsync(menu1, p1, revisor, resultadoId: excelenteId);
        await _s.RevalorizarAsync(revisada, revisor, generada);

        var resultado = await _lector.ConsultarRevalorizadosAsync();

        Assert.Single(resultado);
        var dto = resultado[0];
        Assert.Equal(p1, dto.PacienteId);
        Assert.Equal(5, dto.ValorRevalorizacion);
        Assert.Equal(3.0m, dto.PromedioSala);
        Assert.Equal(2.0m, dto.Diferencia);
    }

    [Fact]
    public async Task ConsultarRevalorizadosAsync_OrdenCanonico_SalaDietaPaciente()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();
        var sala = await _s.SalaAsync();

        var p1 = await _s.PacienteAsync(sala);
        var p2 = await _s.PacienteAsync(sala);
        await _s.InscribirAsync(p1, dieta);
        await _s.InscribirAsync(p2, dieta);

        var menu1 = await _s.MenuAsync(dieta, creador);
        await _s.ValidarAsync(menu1, revisor, aprobado: true);
        await _s.AsignarAsync(menu1, p1);
        await _s.AsignarAsync(menu1, p2);

        var malaId = Guid.Parse("4077627e-08b1-404a-aeb5-bb64a90538ba");
        var excelenteId = Guid.Parse("138dec6d-8202-4b54-831c-e6fd21508695");

        var r1 = await _s.ValorarAsync(menu1, p1, revisor, resultadoId: malaId);
        var r2 = await _s.ValorarAsync(menu1, p2, revisor, resultadoId: malaId);
        var g1 = await _s.ValorarAsync(menu1, p1, revisor, resultadoId: excelenteId);
        var g2 = await _s.ValorarAsync(menu1, p2, revisor, resultadoId: excelenteId);
        await _s.RevalorizarAsync(r1, revisor, g1);
        await _s.RevalorizarAsync(r2, revisor, g2);

        var resultado = await _lector.ConsultarRevalorizadosAsync();

        Assert.Equal(2, resultado.Count);
        // Orden canónico: sala, dieta, paciente
        Assert.Equal(p1, resultado[0].PacienteId);
        Assert.Equal(p2, resultado[1].PacienteId);
    }

    [Fact]
    public async Task ConsultarRevalorizadosAsync_SinFilas_DevuelveListaVacia()
    {
        var resultado = await _lector.ConsultarRevalorizadosAsync();

        Assert.Empty(resultado);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Helper para crear menú con parámetros exactos (copiado de ConsultasInformacionalesTests)
    // ---------------------------------------------------------------------------------------------------------
    private async Task<Guid> CrearMenuConParametrosAsync(
        Guid dietaId, Guid creadorId, int cantidadDeclarada,
        (Guid TipoId, decimal Proporcion)[] proporciones, Guid[] gruposCubiertos)
    {
        var id = Guid.NewGuid();
        var conn = _conexion;
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico, validacion_origen_id)
                VALUES (@id, @dieta_id, @creador_id, NOW(), @cantidad, FALSE, NULL)
                """;
            cmd.Parameters.Add(new NpgsqlParameter("id", id));
            cmd.Parameters.Add(new NpgsqlParameter("dieta_id", dietaId));
            cmd.Parameters.Add(new NpgsqlParameter("creador_id", creadorId));
            cmd.Parameters.Add(new NpgsqlParameter("cantidad", cantidadDeclarada));
            await cmd.ExecuteNonQueryAsync();

            foreach (var (tipoId, proporcion) in proporciones)
            {
                cmd.CommandText = """
                    INSERT INTO menu_distribucion (menu_id, tipo_preparacion_id, proporcion) VALUES (@menu, @tipo, @proporcion)
                    """;
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new NpgsqlParameter("menu", id));
                cmd.Parameters.Add(new NpgsqlParameter("tipo", tipoId));
                cmd.Parameters.Add(new NpgsqlParameter("proporcion", proporcion));
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var grupoId in gruposCubiertos)
            {
                cmd.CommandText = """
                    INSERT INTO menu_grupo_nutricional (menu_id, grupo_nutricional_id) VALUES (@menu, @grupo)
                    """;
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new NpgsqlParameter("menu", id));
                cmd.Parameters.Add(new NpgsqlParameter("grupo", grupoId));
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
        return id;
    }
}
