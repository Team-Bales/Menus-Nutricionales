using System;
using System.Threading.Tasks;
using Npgsql;

namespace Menus.Infrastructure.IntegrationTests.Datos;

/// <summary>
/// Helpers de siembra para las pruebas del submodelo Alimento-Plato (#200). Es un archivo <c>partial</c> propio para
/// no tocar <see cref="Sembrador"/> (#198).
/// </summary>
public sealed partial class Sembrador
{
    /// <summary>
    /// Inserta un ítem con el nombre y la clase indicados, tal cual, sin normalizar ni validar nada: lo tiene que
    /// juzgar la base. No abre una transacción propia, así que la prueba decide si va dentro de una (por ejemplo,
    /// para que el trigger diferido RK-03 salte en el <c>COMMIT</c>) o se confirma sola.
    /// </summary>
    /// <param name="nombre">Nombre del ítem, sin modificar.</param>
    /// <param name="clase">Clase del ítem; los valores válidos son <c>Alimento</c> y <c>Plato</c>.</param>
    /// <param name="nutricionistaId">Nutricionista que registra el ítem; se crea con <see cref="NutricionistaAsync"/>
    /// antes de abrir la transacción, porque Npgsql no admite transacciones anidadas en la misma conexión.</param>
    /// <param name="transaccion">Transacción en la que se inserta, o null para confirmar el <c>INSERT</c> solo.</param>
    /// <returns>El ID del ítem.</returns>
    public async Task<Guid> ItemSinValidarAsync(string nombre, string clase, Guid nutricionistaId, NpgsqlTransaction? transaccion = null)
    {
        var id = Guid.NewGuid();
        await using var cmd = _conexion.CreateCommand();
        cmd.Transaction = transaccion;
        cmd.CommandText = """
            INSERT INTO items_alimentarios (id, nombre, grupo_nutricional_id, tipo_preparacion_id, nivel_calorico_id, nutricionista_id, clase)
            VALUES (@id, @nombre, @grupo_nutricional_id, @tipo_preparacion_id, @nivel_calorico_id, @nutricionista_id, @clase)
            """;
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("nombre", nombre);
        cmd.Parameters.AddWithValue("grupo_nutricional_id", GrupoCereales);
        cmd.Parameters.AddWithValue("tipo_preparacion_id", TipoPlatoFuerte);
        cmd.Parameters.AddWithValue("nivel_calorico_id", NivelMedio);
        cmd.Parameters.AddWithValue("nutricionista_id", nutricionistaId);
        cmd.Parameters.AddWithValue("clase", clase);
        await cmd.ExecuteNonQueryAsync();
        return id;
    }

    /// <summary>
    /// Inserta una fila de <c>composicion</c> sin pasar por el dominio, para que la juzguen el CHECK de la cantidad
    /// (RD-05) y el trigger de un solo nivel (RS-12).
    /// </summary>
    /// <param name="platoId">El compuesto; para RS-12 tiene que ser de clase <c>Plato</c>.</param>
    /// <param name="alimentoId">El componente; para RS-12 tiene que ser de clase <c>Alimento</c>.</param>
    /// <param name="cantidad">Cantidad del componente; RD-05 exige que sea mayor que cero.</param>
    /// <param name="transaccion">Transacción en la que se inserta, o null para confirmar el <c>INSERT</c> solo.</param>
    public async Task ComposicionSinValidarAsync(Guid platoId, Guid alimentoId, decimal cantidad = 100, NpgsqlTransaction? transaccion = null)
    {
        await using var cmd = _conexion.CreateCommand();
        cmd.Transaction = transaccion;
        cmd.CommandText = """
            INSERT INTO composicion (plato_id, alimento_id, cantidad)
            VALUES (@plato_id, @alimento_id, @cantidad)
            """;
        cmd.Parameters.AddWithValue("plato_id", platoId);
        cmd.Parameters.AddWithValue("alimento_id", alimentoId);
        cmd.Parameters.AddWithValue("cantidad", cantidad);
        await cmd.ExecuteNonQueryAsync();
    }
}
