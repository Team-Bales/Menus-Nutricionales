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
    /// Inserta una fila (<c>id</c>, <c>nombre</c>) en uno de los tres catálogos del submodelo, sin validar el nombre:
    /// lo juzgan el CHECK (RD-01, RD-02, RD-04) y el índice único (RD-03, RD-08) de la tabla.
    /// </summary>
    /// <remarks>
    /// El <c>INSERT</c> va en una transacción que siempre se deshace. Los catálogos los siembra la migración y
    /// <see cref="Fixtures.PostgresFixture.ReiniciarAsync"/> no los limpia: si una regresión dejara pasar la fila, se
    /// quedaría en el contenedor compartido y alteraría las demás pruebas. El CHECK y el índice único saltan en el
    /// propio <c>INSERT</c>, así que no hace falta confirmar para que la prueba vea el error.
    /// </remarks>
    /// <param name="tabla">
    /// <c>grupos_nutricionales</c>, <c>tipos_preparacion</c> o <c>niveles_caloricos</c>. Cada una tiene su sentencia
    /// literal, así que el nombre de la tabla nunca se interpola en el SQL.
    /// </param>
    /// <param name="nombre">Nombre de la fila, sin modificar.</param>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="tabla"/> no es uno de los tres catálogos.</exception>
    public async Task CatalogoSinValidarAsync(string tabla, string nombre)
    {
        await using var tx = await _conexion.BeginTransactionAsync();
        await using var cmd = _conexion.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = tabla switch
        {
            "grupos_nutricionales" => "INSERT INTO grupos_nutricionales (id, nombre) VALUES (@id, @nombre)",
            "tipos_preparacion" => "INSERT INTO tipos_preparacion (id, nombre) VALUES (@id, @nombre)",
            "niveles_caloricos" => "INSERT INTO niveles_caloricos (id, nombre) VALUES (@id, @nombre)",
            _ => throw new ArgumentOutOfRangeException(nameof(tabla), tabla, "No es un catálogo del submodelo Alimento-Plato."),
        };
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("nombre", nombre);
        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            await tx.RollbackAsync();
        }
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
