using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;

namespace Menus.Infrastructure.IntegrationTests.Datos;

/// <summary>
/// Extensión del sembrador para las pruebas del submodelo Dieta-Nutricionista-Paciente-Sala (issue #201).
/// </summary>
public sealed partial class Sembrador
{
    /// <summary>
    /// Inserta una fila en <c>restringe</c> vinculando dieta y restricción alimentaria.
    /// </summary>
    public async Task RestringirAsync(Guid dietaId, Guid restriccionAlimentariaId)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO restringe (dieta_id, restriccion_alimentaria_id)
                VALUES (@dieta_id, @restriccion_alimentaria_id)
                """;
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            cmd.Parameters.AddWithValue("restriccion_alimentaria_id", restriccionAlimentariaId);
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
    /// Inserta una fila en <c>excluye</c> vinculando restricción alimentaria y grupo nutricional.
    /// </summary>
    public async Task ExcluirAsync(Guid restriccionAlimentariaId, Guid grupoNutricionalId)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO excluye (restriccion_alimentaria_id, grupo_nutricional_id)
                VALUES (@restriccion_alimentaria_id, @grupo_nutricional_id)
                """;
            cmd.Parameters.AddWithValue("restriccion_alimentaria_id", restriccionAlimentariaId);
            cmd.Parameters.AddWithValue("grupo_nutricional_id", grupoNutricionalId);
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
    /// Inserta una fila en <c>cubre_dieta</c> con el tipo especificado.
    /// </summary>
    public async Task CubrirDietaAsync(Guid dietaId, Guid grupoNutricionalId, string tipo)
    {
        await using var tx = await _conexion.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = _conexion.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO cubre_dieta (dieta_id, grupo_nutricional_id, tipo)
                VALUES (@dieta_id, @grupo_nutricional_id, @tipo)
                """;
            cmd.Parameters.AddWithValue("dieta_id", dietaId);
            cmd.Parameters.AddWithValue("grupo_nutricional_id", grupoNutricionalId);
            cmd.Parameters.AddWithValue("tipo", tipo);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
