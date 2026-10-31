using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;

namespace Menus.Infrastructure.IntegrationTests.Datos;

/// <summary>
/// Sembrador de datos de prueba para la base de datos PostgreSQL.
/// Inserta agregados válidos usando SQL directo con parámetros, respetando los triggers diferidos
/// (RK-03, RK-07, RK-09) que se validan al commit.
/// </summary>
/// <remarks>
/// Cada método abre una transacción, inserta un agregado completo y hace commit.
/// Usa los GUID fijos que siembra la migración <c>InitialCreate</c> para los catálogos:
/// <list type="bullet">
///   <item>Especialidad: <c>44444444-0000-0000-0000-000000000001</c> (Nutrición Clínica)</item>
///   <item>Programa: <c>33333333-0000-0000-0000-000000000001</c> (Nutrición Pediátrica)</item>
///   <item>Grupo "Cereales": <c>11111111-0000-0000-0000-000000000001</c></item>
///   <item>Tipo "Plato fuerte": <c>55555555-0000-0000-0000-000000000002</c></item>
///   <item>Nivel "Medio": <c>66666666-0000-0000-0000-000000000002</c></item>
/// </list>
/// Los métodos que necesiten los issues 06-08 y no estén aquí se añaden en su propio archivo
/// <c>partial</c> sin tocar esta clase.
/// </remarks>
/// <param name="conexion">Conexión Npgsql ya abierta.</param>
public sealed partial class Sembrador(NpgsqlConnection conexion)
{
    private readonly NpgsqlConnection _conexion = conexion;

    // GUIDs fijos de los catálogos sembrados por InitialCreate
    private static readonly Guid EspecialidadClinica = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid ProgramaPediatria = Guid.Parse("33333333-0000-0000-0000-000000000001");
    private static readonly Guid GrupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid TipoPlatoFuerte = Guid.Parse("55555555-0000-0000-0000-000000000002");
    private static readonly Guid NivelMedio = Guid.Parse("66666666-0000-0000-0000-000000000002");

    private static int _contadorNombres = 0;
    private static string NombreUnico(string baseNombre) => $"{baseNombre}_{++_contadorNombres}_{Guid.NewGuid():N[..8]}";

    /// <summary>
    /// Inserta una sala y devuelve su ID.
    /// </summary>
    public async Task<Guid> SalaAsync()
    {
        var id = Guid.NewGuid();
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO salas (id, nombre)
                VALUES (@id, @nombre)
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("nombre", NombreUnico("Sala Test"));
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
        return id;
    }

    /// <summary>
    /// Inserta un nutricionista con la especialidad sembrada y devuelve su ID.
    /// </summary>
    /// <param name="esJefe">Si el nutricionista es jefe (columna <c>es_jefe</c>).</param>
    public async Task<Guid> NutricionistaAsync(bool esJefe = false)
    {
        var id = Guid.NewGuid();
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO nutricionistas (id, nombre, especialidad_id, es_jefe)
                VALUES (@id, @nombre, @especialidad_id, @es_jefe)
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("nombre", NombreUnico("Nutricionista Test"));
            cmd.Parameters.AddWithValue("especialidad_id", EspecialidadClinica);
            cmd.Parameters.AddWithValue("es_jefe", esJefe);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
        return id;
    }

    /// <summary>
    /// Inserta un paciente (crea sala si no se proporciona) y devuelve su ID.
    /// </summary>
    /// <param name="salaId">ID de sala existente; si es null, crea una nueva.</param>
    public async Task<Guid> PacienteAsync(Guid? salaId = null)
    {
        var id = Guid.NewGuid();
        var sala = salaId ?? await SalaAsync();

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO pacientes (id, nombre, edad, sala_id)
                VALUES (@id, @nombre, @edad, @sala_id)
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("nombre", NombreUnico("Paciente Test"));
            cmd.Parameters.AddWithValue("edad", 30);
            cmd.Parameters.AddWithValue("sala_id", sala);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
        return id;
    }

    /// <summary>
    /// Inserta una dieta y su fila en <c>cubre_dieta</c> con tipo <c>Requerido</c> (RK-07).
    /// </summary>
    /// <param name="grupoRequeridoId">Grupo nutricional requerido; por defecto "Cereales".</param>
    /// <returns>ID de la dieta creada.</returns>
    public async Task<Guid> DietaAsync(Guid? grupoRequeridoId = null)
    {
        var id = Guid.NewGuid();
        var grupo = grupoRequeridoId ?? GrupoCereales;

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO dietas (id, nombre, programa_atencion_id)
                VALUES (@id, @nombre, @programa_atencion_id)
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("nombre", NombreUnico("Dieta Test"));
            cmd.Parameters.AddWithValue("programa_atencion_id", ProgramaPediatria);
            await cmd.ExecuteNonQueryAsync();

            cmd.CommandText = """
                INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo)
                VALUES (@dieta_id, @grupo_nutricional_id, 'Requerido')
                """;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("dieta_id", id);
            cmd.Parameters.AddWithValue("grupo_nutricional_id", grupo);
            await cmd.ExecuteNonQueryAsync();

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
        return id;
    }

    /// <summary>
    /// Inserta una fila en <c>autoriza</c> vinculando nutricionista y dieta.
    /// </summary>
    public async Task AutorizarAsync(Guid nutricionistaId, Guid dietaId)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO autoriza (nutricionista_id, dieta_id)
                VALUES (@nutricionista_id, @dieta_id)
                """;
            cmd.Parameters.AddWithValue("nutricionista_id", nutricionistaId);
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Inscribe un paciente en una dieta (fila en <c>paciente_dieta</c>).
    /// </summary>
    public async Task InscribirAsync(Guid pacienteId, Guid dietaId)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO paciente_dieta (paciente_id, dieta_id)
                VALUES (@paciente_id, @dieta_id)
                """;
            cmd.Parameters.AddWithValue("paciente_id", pacienteId);
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Inserta un alimento (clase <c>Alimento</c>) con nombre único y devuelve su ID.
    /// </summary>
    /// <param name="nombre">Nombre base; se genera uno único si es null.</param>
    public async Task<Guid> AlimentoAsync(string? nombre = null)
    {
        var id = Guid.NewGuid();
        var nombreFinal = nombre ?? NombreUnico("Alimento Test");

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO items_alimentarios (id, nombre, grupo_nutricional_id, tipo_preparacion_id, nivel_calorico_id, nutricionista_id, clase)
                VALUES (@id, @nombre, @grupo_nutricional_id, @tipo_preparacion_id, @nivel_calorico_id, @nutricionista_id, 'Alimento')
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("nombre", nombreFinal);
            cmd.Parameters.AddWithValue("grupo_nutricional_id", GrupoCereales);
            cmd.Parameters.AddWithValue("tipo_preparacion_id", TipoPlatoFuerte); // temporal, se puede cambiar
            cmd.Parameters.AddWithValue("nivel_calorico_id", NivelMedio);
            cmd.Parameters.AddWithValue("nutricionista_id", await NutricionistaAsync()); // crea uno nuevo
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
        return id;
    }

    /// <summary>
    /// Inserta un plato (clase <c>Plato</c>) y su composición con al menos un alimento (RK-03).
    /// </summary>
    /// <param name="alimentos">IDs de alimentos que componen el plato (mínimo 1).</param>
    /// <returns>ID del plato creado.</returns>
    public async Task<Guid> PlatoAsync(params Guid[] alimentos)
    {
        if (alimentos.Length == 0)
        {
            throw new ArgumentException("Un plato debe tener al menos un alimento (RK-03).", nameof(alimentos));
        }

        var id = Guid.NewGuid();

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
            cmd.Parameters.AddWithValue("tipo_preparacion_id", TipoPlatoFuerte);
            cmd.Parameters.AddWithValue("nivel_calorico_id", NivelMedio);
            cmd.Parameters.AddWithValue("nutricionista_id", await NutricionistaAsync()); // crea uno nuevo
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
            await tx.RollbackAsync();
            throw;
        }
        return id;
    }

    /// <summary>
    /// Inserta un menú completo con distribución (tipo "Plato fuerte", proporción 1) y grupo nutricional (RK-09).
    /// El creador debe estar autorizado en la dieta (trigger RS-03 / trg_r6_22_creador).
    /// </summary>
    /// <param name="dietaId">ID de la dieta.</param>
    /// <param name="creadorId">ID del nutricionista creador (debe estar autorizado en la dieta).</param>
    /// <returns>ID del menú creado.</returns>
    public async Task<Guid> MenuAsync(Guid dietaId, Guid creadorId)
    {
        var id = Guid.NewGuid();

        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO menus (id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, es_automatico, validacion_origen_id)
                VALUES (@id, @dieta_id, @nutricionista_creador_id, NOW() - INTERVAL '1 day', 1, FALSE, NULL)
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            cmd.Parameters.AddWithValue("nutricionista_creador_id", creadorId);
            await cmd.ExecuteNonQueryAsync();

            // menu_distribucion: proporción 1, tipo "Plato fuerte"
            cmd.CommandText = """
                INSERT INTO menu_distribucion (menu_id, tipo_preparacion_id, proporcion)
                VALUES (@menu_id, @tipo_preparacion_id, 1.0)
                """;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("menu_id", id);
            cmd.Parameters.AddWithValue("tipo_preparacion_id", TipoPlatoFuerte);
            await cmd.ExecuteNonQueryAsync();

            // menu_grupo_nutricional: RK-09 (al menos el grupo requerido de la dieta)
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
            await tx.RollbackAsync();
            throw;
        }
        return id;
    }
}
