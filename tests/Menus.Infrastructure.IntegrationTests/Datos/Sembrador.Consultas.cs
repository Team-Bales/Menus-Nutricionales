using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;

namespace Menus.Infrastructure.IntegrationTests.Datos;

/// <summary>
/// Helpers de siembra para las pruebas de las consultas informacionales 1 a 6 (#102, #217): ítems con nivel
/// calórico y grupo elegidos, validaciones, menús de reemplazo, asignaciones, servicios, consumos, valoraciones y
/// revalorizaciones. No toca <see cref="Sembrador"/>: es un archivo <c>partial</c> propio, como indica su
/// comentario.
/// </summary>
public sealed partial class Sembrador
{
    // GUIDs fijos de los catálogos sembrados por InitialCreate, además de los que ya expone Sembrador.cs.
    // Internos (no privados) porque las pruebas de Menus.Infrastructure.IntegrationTests los usan para armar
    // escenarios con un grupo, un nivel o un tipo concretos.
    internal static readonly Guid GrupoProteinas = Guid.Parse("11111111-0000-0000-0000-000000000002");
    internal static readonly Guid NivelBajo = Guid.Parse("66666666-0000-0000-0000-000000000001");
    internal static readonly Guid NivelAlto = Guid.Parse("66666666-0000-0000-0000-000000000003");
    internal static readonly Guid TipoEntrante = Guid.Parse("55555555-0000-0000-0000-000000000001");

    /// <summary>Resultado nutricional "Normal" (valor 3), usado por defecto en las valoraciones de prueba.</summary>
    private static readonly Guid ResultadoNormal = Guid.Parse("023f26c4-50c4-4e20-8c24-7d27b341dfd6");

    // Mismos valores que GrupoCereales, TipoPlatoFuerte y NivelMedio de Sembrador.cs (privados en ese archivo),
    // repetidos como literal aquí para que las pruebas los usen sin depender del orden de inicialización entre
    // archivos parciales.
    internal static readonly Guid GrupoCerealesId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    internal static readonly Guid TipoPlatoFuerteId = Guid.Parse("55555555-0000-0000-0000-000000000002");
    internal static readonly Guid NivelMedioId = Guid.Parse("66666666-0000-0000-0000-000000000002");

    /// <summary>
    /// Inserta un ítem (alimento o plato) con el grupo, el tipo y el nivel calórico indicados.
    /// </summary>
    public async Task<Guid> ItemAsync(string clase, Guid? grupoId = null, Guid? tipoId = null, Guid? nivelId = null, string? nombre = null)
    {
        var id = Guid.NewGuid();
        var nutricionistaId = await NutricionistaAsync(); // antes de abrir la transacción: Npgsql no admite transacciones anidadas en la misma conexión
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO items_alimentarios (id, nombre, grupo_nutricional_id, tipo_preparacion_id, nivel_calorico_id, nutricionista_id, clase)
                VALUES (@id, @nombre, @grupo_nutricional_id, @tipo_preparacion_id, @nivel_calorico_id, @nutricionista_id, @clase)
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("nombre", nombre ?? NombreUnico($"{clase} Test"));
            cmd.Parameters.AddWithValue("grupo_nutricional_id", grupoId ?? GrupoCereales);
            cmd.Parameters.AddWithValue("tipo_preparacion_id", tipoId ?? TipoPlatoFuerte);
            cmd.Parameters.AddWithValue("nivel_calorico_id", nivelId ?? NivelMedio);
            cmd.Parameters.AddWithValue("nutricionista_id", nutricionistaId);
            cmd.Parameters.AddWithValue("clase", clase);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
        return id;
    }

    /// <summary>Inserta un alimento con el grupo y el nivel calórico indicados.</summary>
    public Task<Guid> AlimentoConAsync(Guid? grupoId = null, Guid? nivelId = null, string? nombre = null)
        => ItemAsync("Alimento", grupoId, nivelId: nivelId, nombre: nombre);

    /// <summary>
    /// Inserta un plato con el tipo y el nivel calórico indicados, y su composición, en la misma transacción
    /// (RK-03 es un trigger diferido: si el plato y su composición no se confirman juntos, el plato queda sin
    /// alimentos al cerrar la transacción del alta y el trigger la rechaza).
    /// </summary>
    public async Task<Guid> PlatoConAsync(Guid? tipoId, Guid? nivelId, params Guid[] alimentos)
    {
        if (alimentos.Length == 0)
        {
            throw new ArgumentException("Un plato debe tener al menos un alimento (RK-03).", nameof(alimentos));
        }

        var id = Guid.NewGuid();
        var nutricionistaId = await NutricionistaAsync();
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO items_alimentarios (id, nombre, grupo_nutricional_id, tipo_preparacion_id, nivel_calorico_id, nutricionista_id, clase)
                VALUES (@id, @nombre, @grupo_nutricional_id, @tipo_preparacion_id, @nivel_calorico_id, @nutricionista_id, 'Plato')
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("nombre", NombreUnico("Plato Test"));
            cmd.Parameters.AddWithValue("grupo_nutricional_id", GrupoCereales);
            cmd.Parameters.AddWithValue("tipo_preparacion_id", tipoId ?? TipoPlatoFuerte);
            cmd.Parameters.AddWithValue("nivel_calorico_id", nivelId ?? NivelMedio);
            cmd.Parameters.AddWithValue("nutricionista_id", nutricionistaId);
            await cmd.ExecuteNonQueryAsync();

            foreach (var alimentoId in alimentos)
            {
                cmd.CommandText = """
                    INSERT INTO composicion (plato_id, alimento_id, cantidad)
                    VALUES (@plato_id, @alimento_id, 100)
                    """;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("plato_id", id);
                cmd.Parameters.AddWithValue("alimento_id", alimentoId);
                await cmd.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
        return id;
    }

    /// <summary>Incluye uno o más ítems (alimentos o platos) directamente en un menú (<c>menu_item</c>).</summary>
    public async Task IncluirEnMenuAsync(Guid menuId, params Guid[] itemIds)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            foreach (var itemId in itemIds)
            {
                cmd.CommandText = """
                    INSERT INTO menu_item (menu_id, item_id)
                    VALUES (@menu_id, @item_id)
                    """;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("menu_id", menuId);
                cmd.Parameters.AddWithValue("item_id", itemId);
                await cmd.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
    }

    /// <summary>
    /// Autoriza al nutricionista para la dieta (tabla <c>autoriza</c>), requisito de RS-03 tanto para crear
    /// como para revisar un menú de esa dieta.
    /// </summary>
    public Task AutorizarParaDietaAsync(Guid nutricionistaId, Guid dietaId) => AutorizarAsync(nutricionistaId, dietaId);

    /// <summary>
    /// Registra el dictamen (aprobación o rechazo) de un menú. El revisor debe ser jefe (RS-03) y estar
    /// autorizado para la dieta del menú, y no puede ser su creador (RS-05).
    /// </summary>
    public async Task ValidarAsync(Guid menuId, Guid revisorId, bool aprobado, DateTime? fechaValidacion = null, string? observaciones = null)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO validacion (menu_id, nutricionista_id, fecha_validacion, aprobado, observaciones)
                VALUES (@menu_id, @nutricionista_id, @fecha_validacion, @aprobado, @observaciones)
                """;
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("nutricionista_id", revisorId);
            cmd.Parameters.AddWithValue("fecha_validacion", (fechaValidacion ?? DateTime.UtcNow).ToUniversalTime());
            cmd.Parameters.AddWithValue("aprobado", aprobado);
            cmd.Parameters.AddWithValue("observaciones", (object?)observaciones ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
    }

    /// <summary>
    /// Inserta un menú de reemplazo de <paramref name="menuRechazadoId"/> (relación <c>Origina</c>, RS-17/RS-18):
    /// el menú rechazado debe tener su dictamen registrado con <c>aprobado = false</c>, y el reemplazo es de la
    /// misma dieta.
    /// </summary>
    public async Task<Guid> MenuDeReemplazoAsync(Guid dietaId, Guid creadorId, Guid menuRechazadoId)
    {
        var id = Guid.NewGuid();
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico, validacion_origen_id)
                VALUES (@id, @dieta_id, @nutricionista_creador_id, NOW(), 1, FALSE, @validacion_origen_id)
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            cmd.Parameters.AddWithValue("nutricionista_creador_id", creadorId);
            cmd.Parameters.AddWithValue("validacion_origen_id", menuRechazadoId);
            await cmd.ExecuteNonQueryAsync();

            cmd.CommandText = """
                INSERT INTO menu_distribucion (menu_id, tipo_preparacion_id, proporcion)
                VALUES (@menu_id, @tipo_preparacion_id, 1.0)
                """;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("menu_id", id);
            cmd.Parameters.AddWithValue("tipo_preparacion_id", TipoPlatoFuerte);
            await cmd.ExecuteNonQueryAsync();

            cmd.CommandText = """
                INSERT INTO menu_grupo_nutricional (menu_id, grupo_nutricional_id)
                VALUES (@menu_id, @grupo_nutricional_id)
                """;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("menu_id", id);
            cmd.Parameters.AddWithValue("grupo_nutricional_id", GrupoCereales);
            await cmd.ExecuteNonQueryAsync();

            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
        return id;
    }

    /// <summary>Inserta una asignación (menú aprobado → paciente inscrito en la dieta del menú, RS-04/RS-16).</summary>
    public async Task AsignarAsync(Guid menuId, Guid pacienteId)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO asignacion (menu_id, paciente_id)
                VALUES (@menu_id, @paciente_id)
                """;
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("paciente_id", pacienteId);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
    }

    /// <summary>Registra el servicio de una asignación en una fecha (RS-14: no antes de la aprobación).</summary>
    public async Task ServirAsync(Guid menuId, Guid pacienteId, DateOnly fechaServicio)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO servicios (menu_id, paciente_id, fecha_servicio)
                VALUES (@menu_id, @paciente_id, @fecha_servicio)
                """;
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("paciente_id", pacienteId);
            cmd.Parameters.AddWithValue("fecha_servicio", fechaServicio.ToDateTime(TimeOnly.MinValue));
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
    }

    /// <summary>Registra el consumo (aceptado o rechazado) de un ítem incluido en el menú, en un servicio.</summary>
    public async Task ConsumirAsync(Guid menuId, Guid pacienteId, DateOnly fechaServicio, Guid itemId, bool aceptacion)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO consumo (menu_id, paciente_id, fecha_servicio, item_id, aceptacion)
                VALUES (@menu_id, @paciente_id, @fecha_servicio, @item_id, @aceptacion)
                """;
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("paciente_id", pacienteId);
            cmd.Parameters.AddWithValue("fecha_servicio", fechaServicio.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("item_id", itemId);
            cmd.Parameters.AddWithValue("aceptacion", aceptacion);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
    }

    /// <summary>Inserta una valoración nutricional (RR-17) y devuelve su ID.</summary>
    public async Task<Guid> ValorarAsync(Guid menuId, Guid pacienteId, Guid nutricionistaId, DateTime? fechaValoracion = null, Guid? resultadoId = null)
    {
        var id = Guid.NewGuid();
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO valoraciones_nutricionales (id, menu_id, paciente_id, nutricionista_id, resultado_nutricional_id, fecha_valoracion)
                VALUES (@id, @menu_id, @paciente_id, @nutricionista_id, @resultado_nutricional_id, @fecha_valoracion)
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("menu_id", menuId);
            cmd.Parameters.AddWithValue("paciente_id", pacienteId);
            cmd.Parameters.AddWithValue("nutricionista_id", nutricionistaId);
            cmd.Parameters.AddWithValue("resultado_nutricional_id", resultadoId ?? ResultadoNormal);
            cmd.Parameters.AddWithValue("fecha_valoracion", (fechaValoracion ?? DateTime.UtcNow).ToUniversalTime());
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
        return id;
    }

    /// <summary>
    /// Inserta una revalorización. Si <paramref name="valoracionGeneradaId"/> es <see langword="null"/>, queda
    /// pendiente (sin generar todavía su valoración).
    /// </summary>
    public async Task<Guid> RevalorizarAsync(Guid valoracionRevisadaId, Guid nutricionistaId, Guid? valoracionGeneradaId = null, DateTime? fechaSolicitud = null)
    {
        var id = Guid.NewGuid();
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO revalorizaciones (id, fecha_solicitud, nutricionista_id, valoracion_revisada_id, valoracion_generada_id)
                VALUES (@id, @fecha_solicitud, @nutricionista_id, @valoracion_revisada_id, @valoracion_generada_id)
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("fecha_solicitud", (fechaSolicitud ?? DateTime.UtcNow).ToUniversalTime());
            cmd.Parameters.AddWithValue("nutricionista_id", nutricionistaId);
            cmd.Parameters.AddWithValue("valoracion_revisada_id", valoracionRevisadaId);
            cmd.Parameters.AddWithValue("valoracion_generada_id", (object?)valoracionGeneradaId ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
        return id;
    }

    /// <summary>Hace jefe a un nutricionista existente (columna <c>es_jefe</c>), requisito de RS-03 para dictaminar.</summary>
    public async Task HacerJefeAsync(Guid nutricionistaId)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE nutricionistas SET es_jefe = TRUE WHERE id = @id";
            cmd.Parameters.AddWithValue("id", nutricionistaId);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
    }

    /// <summary>
    /// Inserta un menú con la parametrización indicada (distribución por tipo de preparación y grupos cubiertos),
    /// para las pruebas de la consulta 1. A diferencia de <see cref="MenuAsync"/>, permite elegir la fecha de
    /// creación, si es automático y una distribución/cobertura con más de una fila.
    /// </summary>
    public async Task<Guid> MenuConParametrosAsync(
        Guid dietaId,
        Guid creadorId,
        DateTime fechaCreacion,
        int cantidadTotalAlimentos,
        bool esAutomatico,
        (Guid TipoPreparacionId, decimal Proporcion)[] distribucion,
        Guid[] grupos)
    {
        var id = Guid.NewGuid();
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico, validacion_origen_id)
                VALUES (@id, @dieta_id, @nutricionista_creador_id, @fecha_creacion, @cantidad_total_alimentos, @es_automatico, NULL)
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            cmd.Parameters.AddWithValue("nutricionista_creador_id", creadorId);
            cmd.Parameters.AddWithValue("fecha_creacion", fechaCreacion.ToUniversalTime());
            cmd.Parameters.AddWithValue("cantidad_total_alimentos", cantidadTotalAlimentos);
            cmd.Parameters.AddWithValue("es_automatico", esAutomatico);
            await cmd.ExecuteNonQueryAsync();

            foreach (var (tipoPreparacionId, proporcion) in distribucion)
            {
                cmd.CommandText = """
                    INSERT INTO menu_distribucion (menu_id, tipo_preparacion_id, proporcion)
                    VALUES (@menu_id, @tipo_preparacion_id, @proporcion)
                    """;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("menu_id", id);
                cmd.Parameters.AddWithValue("tipo_preparacion_id", tipoPreparacionId);
                cmd.Parameters.AddWithValue("proporcion", proporcion);
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var grupoId in grupos)
            {
                cmd.CommandText = """
                    INSERT INTO menu_grupo_nutricional (menu_id, grupo_nutricional_id)
                    VALUES (@menu_id, @grupo_nutricional_id)
                    """;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("menu_id", id);
                cmd.Parameters.AddWithValue("grupo_nutricional_id", grupoId);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* no tapar la excepción original */ }
            throw;
        }
        return id;
    }
}
