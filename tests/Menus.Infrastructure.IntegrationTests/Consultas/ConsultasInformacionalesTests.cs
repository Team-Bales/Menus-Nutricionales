using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Consultas;

/// <summary>
/// Pruebas de las 9 funciones de las consultas informacionales 2 a 6 del informe (§2.2, #217) contra PostgreSQL
/// real. Cada prueba siembra el escenario mínimo que permite calcular el resultado a mano y lo compara con lo
/// que devuelve la función.
/// </summary>
[Collection("PostgreSQL")]
public sealed class ConsultasInformacionalesTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _s = null!;

    public ConsultasInformacionalesTests(PostgresFixture fixture)
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

    private async Task<NpgsqlDataReader> EjecutarAsync(string sql, Action<NpgsqlParameterCollection> parametros)
    {
        var cmd = _conexion.CreateCommand();
        cmd.CommandText = sql;
        parametros(cmd.Parameters);
        return await cmd.ExecuteReaderAsync();
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 2: alimentos más usados en los menús finales de una dieta.
    // ---------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Consulta_2_cuenta_una_vez_por_menu_un_alimento_directo_y_por_plato()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();

        var alimentoDirecto = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoProteinas, nivelId: Sembrador.NivelAlto);
        var alimentoEnPlato = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoCerealesId, nivelId: Sembrador.NivelBajo);
        var plato = await _s.PlatoConAsync(tipoId: null, nivelId: Sembrador.NivelBajo, alimentoEnPlato);

        // menú final: incluye el alimento directo, el mismo alimento otra vez vía el plato (no debe duplicar) y el plato.
        var menuFinal = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menuFinal, alimentoDirecto, alimentoEnPlato, plato);
        await _s.ValidarAsync(menuFinal, revisor, aprobado: true);

        // menú pendiente (no final): no debe contar, aunque incluya el mismo alimento directo.
        var menuPendiente = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menuPendiente, alimentoDirecto);

        var filas = new Dictionary<Guid, long>();
        await using var reader = await EjecutarAsync(
            "SELECT * FROM consulta_2_alimentos_mas_usados(@dieta, NULL)",
            p => p.AddWithValue("dieta", dieta));
        while (await reader.ReadAsync())
        {
            filas[reader.GetGuid(0)] = reader.GetInt64(4);
        }

        Assert.Equal(2, filas.Count);
        Assert.Equal(1, filas[alimentoDirecto]);
        Assert.Equal(1, filas[alimentoEnPlato]); // una sola vez, no dos, pese a llegar por las dos vías
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 3: dictámenes de un revisor.
    // ---------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Consulta_3_lista_aprobaciones_y_rechazos_con_el_reemplazo_del_rechazo()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();

        var aprobado = await _s.MenuAsync(dieta, creador);
        await _s.ValidarAsync(aprobado, revisor, aprobado: true, observaciones: "cumple");

        var rechazado = await _s.MenuAsync(dieta, creador);
        await _s.ValidarAsync(rechazado, revisor, aprobado: false, observaciones: "falta proteína");
        var reemplazo = await _s.MenuDeReemplazoAsync(dieta, creador, rechazado);

        var porMenu = new Dictionary<Guid, (bool Aprobado, Guid? Reemplazo, string Observaciones)>();
        await using var reader = await EjecutarAsync(
            "SELECT * FROM consulta_3_dictamenes_de_un_revisor(@revisor)",
            p => p.AddWithValue("revisor", revisor));
        while (await reader.ReadAsync())
        {
            var menuId = reader.GetGuid(0);
            var esAprobado = reader.GetBoolean(5);
            var observaciones = reader.GetString(6);
            Guid? menuReemplazoId = reader.IsDBNull(7) ? null : reader.GetGuid(7);
            porMenu[menuId] = (esAprobado, menuReemplazoId, observaciones);
        }

        Assert.Equal(2, porMenu.Count);
        Assert.True(porMenu[aprobado].Aprobado);
        Assert.Null(porMenu[aprobado].Reemplazo);
        Assert.False(porMenu[rechazado].Aprobado);
        Assert.Equal("falta proteína", porMenu[rechazado].Observaciones);
        Assert.Equal(reemplazo, porMenu[rechazado].Reemplazo);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 4: desempeño ante un menú, por ítem y nivel calórico.
    // ---------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Consulta_4_pondera_la_tasa_por_servicio_y_ordena_por_nivel_calorico()
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

        // itemBajo: 2 servicios con consumo, 2 aceptaciones -> tasa 1.0
        await _s.ConsumirAsync(menu, paciente1, fecha, itemBajo, aceptacion: true);
        await _s.ConsumirAsync(menu, paciente2, fecha, itemBajo, aceptacion: true);
        // itemAlto: 1 servicio con consumo, 0 aceptaciones -> tasa 0.0
        await _s.ConsumirAsync(menu, paciente1, fecha, itemAlto, aceptacion: false);

        var filas = new List<(Guid Item, long ServiciosConConsumo, long Aceptaciones, long Rechazos, decimal? Tasa)>();
        await using var reader = await EjecutarAsync(
            "SELECT * FROM consulta_4_desempeno_ante_un_menu(@menu)",
            p => p.AddWithValue("menu", menu));
        while (await reader.ReadAsync())
        {
            filas.Add((
                reader.GetGuid(0),
                reader.GetInt64(4),
                reader.GetInt64(5),
                reader.GetInt64(6),
                reader.IsDBNull(7) ? null : reader.GetDecimal(7)));
        }

        // orden: Bajo antes que Alto
        Assert.Equal(itemBajo, filas[0].Item);
        Assert.Equal(2, filas[0].ServiciosConConsumo);
        Assert.Equal(2, filas[0].Aceptaciones);
        Assert.Equal(0, filas[0].Rechazos);
        Assert.Equal(1.0m, filas[0].Tasa);

        Assert.Equal(itemAlto, filas[1].Item);
        Assert.Equal(1, filas[1].ServiciosConConsumo);
        Assert.Equal(0, filas[1].Aceptaciones);
        Assert.Equal(1, filas[1].Rechazos);
        Assert.Equal(0.0m, filas[1].Tasa);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 5 (1/3): distribución porcentual por grupo nutricional.
    // ---------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Consulta_5_distribucion_por_grupo_calcula_el_porcentaje_correcto()
    {
        var (dieta, creador, _) = await EscenarioBaseAsync();

        var cereal1 = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoCerealesId);
        var cereal2 = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoCerealesId);
        var proteina = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoProteinas);

        var menu = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menu, cereal1, cereal2, proteina);

        var porcentajes = new Dictionary<string, decimal>();
        await using var reader = await EjecutarAsync(
            "SELECT * FROM consulta_5_distribucion_por_grupo(@dietas, FALSE)",
            p => p.AddWithValue("dietas", new[] { dieta }));
        while (await reader.ReadAsync())
        {
            porcentajes[reader.GetString(2)] = reader.GetDecimal(4);
        }

        Assert.Equal(66.67m, porcentajes["Cereales"]);
        Assert.Equal(33.33m, porcentajes["Proteínas"]);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 5 (2/3): distribución porcentual por nivel calórico.
    // ---------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Consulta_5_distribucion_por_nivel_calcula_el_porcentaje_correcto()
    {
        var (dieta, creador, _) = await EscenarioBaseAsync();

        var bajo1 = await _s.AlimentoConAsync(nivelId: Sembrador.NivelBajo);
        var bajo2 = await _s.AlimentoConAsync(nivelId: Sembrador.NivelBajo);
        var alto = await _s.AlimentoConAsync(nivelId: Sembrador.NivelAlto);

        var menu = await _s.MenuAsync(dieta, creador);
        await _s.IncluirEnMenuAsync(menu, bajo1, bajo2, alto);

        var porcentajes = new Dictionary<string, decimal>();
        await using var reader = await EjecutarAsync(
            "SELECT * FROM consulta_5_distribucion_por_nivel(@dietas, FALSE)",
            p => p.AddWithValue("dietas", new[] { dieta }));
        while (await reader.ReadAsync())
        {
            porcentajes[reader.GetString(2)] = reader.GetDecimal(4);
        }

        Assert.Equal(66.67m, porcentajes["Bajo"]);
        Assert.Equal(33.33m, porcentajes["Alto"]);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 5 (3/3): criterios de equilibrio, en los dos sentidos (cumple / no cumple).
    // ---------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Consulta_5_criterios_de_equilibrio_distingue_el_menu_que_cumple_del_que_no()
    {
        var dieta = await _s.DietaAsync(grupoRequeridoId: Sembrador.GrupoCerealesId); // Cereales: Requerido
        var creador = await _s.NutricionistaAsync();
        await _s.AutorizarParaDietaAsync(creador, dieta);

        // Proteínas queda prohibida para esta dieta (cobertura Restringido).
        await using (var cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo) VALUES (@dieta, @grupo, 'Restringido')
                """;
            cmd.Parameters.AddWithValue("dieta", dieta);
            cmd.Parameters.AddWithValue("grupo", Sembrador.GrupoProteinas);
            await cmd.ExecuteNonQueryAsync();
        }

        // Menú que cumple: 2 ítems de Cereales (uno Plato fuerte, uno Entrante), cantidad declarada 2,
        // proporciones declaradas 0.5/0.5, cubre el grupo requerido y no incluye el prohibido.
        var itemPlatoFuerte = await _s.AlimentoConAsync(grupoId: Sembrador.GrupoCerealesId); // tipo por defecto: Plato fuerte
        var itemEntrante = await _s.ItemAsync("Alimento", grupoId: Sembrador.GrupoCerealesId, tipoId: Sembrador.TipoEntrante);
        var menuOk = await CrearMenuConParametrosAsync(dieta, creador, cantidadDeclarada: 2,
            proporciones: [(Sembrador.TipoPlatoFuerteId, 0.5m), (Sembrador.TipoEntrante, 0.5m)],
            gruposCubiertos: [Sembrador.GrupoCerealesId]);
        await _s.IncluirEnMenuAsync(menuOk, itemPlatoFuerte, itemEntrante);

        // Menú que no cumple: cantidad declarada 5 pero solo 1 ítem, de Proteínas (prohibido), sin cubrir Cereales,
        // y una proporción declarada (Plato fuerte 1.0) que el único ítem (Entrante) no satisface.
        var itemProteina = await _s.ItemAsync("Alimento", grupoId: Sembrador.GrupoProteinas, tipoId: Sembrador.TipoEntrante);
        var menuMal = await CrearMenuConParametrosAsync(dieta, creador, cantidadDeclarada: 5,
            proporciones: [(Sembrador.TipoPlatoFuerteId, 1.0m)],
            gruposCubiertos: [Sembrador.GrupoProteinas]);
        await _s.IncluirEnMenuAsync(menuMal, itemProteina);

        var resultados = new Dictionary<Guid, (bool Cantidad, bool Proporciones, bool Requeridos, bool Prohibidos)>();
        await using var reader = await EjecutarAsync(
            "SELECT * FROM consulta_5_criterios_de_equilibrio(@dietas, FALSE)",
            p => p.AddWithValue("dietas", new[] { dieta }));
        while (await reader.ReadAsync())
        {
            resultados[reader.GetGuid(2)] = (reader.GetBoolean(6), reader.GetBoolean(7), reader.GetBoolean(9), reader.GetBoolean(11));
        }

        Assert.True(resultados[menuOk].Cantidad);
        Assert.True(resultados[menuOk].Proporciones);
        Assert.True(resultados[menuOk].Requeridos);
        Assert.True(resultados[menuOk].Prohibidos);

        Assert.False(resultados[menuMal].Cantidad);
        Assert.False(resultados[menuMal].Proporciones);
        Assert.False(resultados[menuMal].Requeridos);
        Assert.False(resultados[menuMal].Prohibidos);
    }

    /// <summary>
    /// Crea un menú con cantidad declarada, proporciones y grupos cubiertos exactos (sin los valores por
    /// defecto de <see cref="Sembrador.MenuAsync"/>), para controlar con precisión lo que evalúa la consulta 5c.
    /// </summary>
    private async Task<Guid> CrearMenuConParametrosAsync(
        Guid dietaId, Guid creadorId, int cantidadDeclarada,
        (Guid TipoId, decimal Proporcion)[] proporciones, Guid[] gruposCubiertos)
    {
        var id = Guid.NewGuid();
        await using var tx = await _conexion.BeginTransactionAsync();
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico, validacion_origen_id)
                VALUES (@id, @dieta_id, @creador_id, NOW(), @cantidad, FALSE, NULL)
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            cmd.Parameters.AddWithValue("creador_id", creadorId);
            cmd.Parameters.AddWithValue("cantidad", cantidadDeclarada);
            await cmd.ExecuteNonQueryAsync();

            foreach (var (tipoId, proporcion) in proporciones)
            {
                cmd.CommandText = """
                    INSERT INTO menu_distribucion (menu_id, tipo_preparacion_id, proporcion) VALUES (@menu, @tipo, @proporcion)
                    """;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("menu", id);
                cmd.Parameters.AddWithValue("tipo", tipoId);
                cmd.Parameters.AddWithValue("proporcion", proporcion);
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var grupoId in gruposCubiertos)
            {
                cmd.CommandText = """
                    INSERT INTO menu_grupo_nutricional (menu_id, grupo_nutricional_id) VALUES (@menu, @grupo)
                    """;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("menu", id);
                cmd.Parameters.AddWithValue("grupo", grupoId);
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

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 6A: correlación de Spearman entre nivel calórico y resultado nutricional, por dieta.
    // ---------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Consulta_6a_da_correlacion_perfecta_cuando_el_orden_coincide()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();
        var paciente = await _s.PacienteAsync();
        await _s.InscribirAsync(paciente, dieta);

        // Bajo -> Muy mala(1), Medio -> Normal(3), Alto -> Excelente(5): orden perfectamente creciente.
        var resultados = new (Guid Nivel, Guid Resultado)[]
        {
            (Sembrador.NivelBajo, Guid.Parse("e89977e7-eeb6-4d32-96e2-096ace535bc8")), // Muy mala
            (Sembrador.NivelMedioId, Guid.Parse("023f26c4-50c4-4e20-8c24-7d27b341dfd6")), // Normal
            (Sembrador.NivelAlto, Guid.Parse("138dec6d-8202-4b54-831c-e6fd21508695")), // Excelente
        };

        foreach (var (nivel, resultado) in resultados)
        {
            var item = await _s.AlimentoConAsync(nivelId: nivel);
            var menu = await _s.MenuAsync(dieta, creador);
            await _s.IncluirEnMenuAsync(menu, item);
            await _s.ValidarAsync(menu, revisor, aprobado: true);
            await _s.AsignarAsync(menu, paciente);
            await _s.ValorarAsync(menu, paciente, revisor, resultadoId: resultado);
        }

        await using var reader = await EjecutarAsync(
            "SELECT * FROM consulta_6a_correlacion_por_dieta(NULL, NULL) WHERE dieta_id = @dieta",
            p => p.AddWithValue("dieta", dieta));
        Assert.True(await reader.ReadAsync());
        Assert.Equal(3, reader.GetInt64(2));
        Assert.Equal(1.0m, reader.GetDecimal(3));
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 6B: los ítems con mayor tasa de rechazo.
    // ---------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Consulta_6b_calcula_la_tasa_de_rechazo_y_lista_las_dietas_del_rechazo()
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
        await _s.ConsumirAsync(menu, paciente, dia1, item, aceptacion: false); // rechazado
        await _s.ConsumirAsync(menu, paciente, dia2, item, aceptacion: true); // aceptado

        await using var reader = await EjecutarAsync(
            "SELECT * FROM consulta_6b_items_mas_rechazados(10, 1)",
            _ => { });
        Assert.True(await reader.ReadAsync());
        Assert.Equal(item, reader.GetGuid(0));
        Assert.Equal(2, reader.GetInt64(4)); // consumos
        Assert.Equal(1, reader.GetInt64(5)); // rechazos
        Assert.Equal(0.5m, reader.GetDecimal(6)); // tasa_rechazo
        Assert.False(await reader.ReadAsync()); // es el único ítem con algún rechazo en la base
    }

    // ---------------------------------------------------------------------------------------------------------
    // Consulta 6C: revalorizados frente al promedio de su sala.
    // ---------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Consulta_6c_compara_contra_el_promedio_de_los_demas_pacientes_de_la_sala()
    {
        var (dieta, creador, revisor) = await EscenarioBaseAsync();
        var sala = await _s.SalaAsync();

        var p1 = await _s.PacienteAsync(sala); // el que se revaloriza
        var p2 = await _s.PacienteAsync(sala); // "los demás" de la sala
        var p3 = await _s.PacienteAsync(sala);
        await _s.InscribirAsync(p1, dieta);
        await _s.InscribirAsync(p2, dieta);
        await _s.InscribirAsync(p3, dieta);

        var menu1 = await _s.MenuAsync(dieta, creador);
        await _s.ValidarAsync(menu1, revisor, aprobado: true);
        await _s.AsignarAsync(menu1, p1);
        await _s.AsignarAsync(menu1, p2);
        await _s.AsignarAsync(menu1, p3);

        var malaId = Guid.Parse("4077627e-08b1-404a-aeb5-bb64a90538ba"); // Mala = 2
        var buenaId = Guid.Parse("c0cf2cb9-45bb-4a30-ab00-0c6a8bfc5ca1"); // Buena = 4
        var excelenteId = Guid.Parse("138dec6d-8202-4b54-831c-e6fd21508695"); // Excelente = 5

        var revisada = await _s.ValorarAsync(menu1, p1, revisor, resultadoId: malaId);
        await _s.ValorarAsync(menu1, p2, revisor, resultadoId: malaId); // 2
        await _s.ValorarAsync(menu1, p3, revisor, resultadoId: buenaId); // 4 -> promedio de "los demás" = 3

        var generada = await _s.ValorarAsync(menu1, p1, revisor, resultadoId: excelenteId); // 5
        await _s.RevalorizarAsync(revisada, revisor, generada);

        await using var reader = await EjecutarAsync(
            "SELECT * FROM consulta_6c_revalorizados_frente_a_su_sala()",
            _ => { });
        Assert.True(await reader.ReadAsync());
        Assert.Equal(p1, reader.GetGuid(1));
        Assert.Equal(5, reader.GetInt32(5));
        Assert.Equal(3.0m, reader.GetDecimal(6));
        Assert.Equal(2.0m, reader.GetDecimal(7));
        Assert.False(await reader.ReadAsync());
    }
}
