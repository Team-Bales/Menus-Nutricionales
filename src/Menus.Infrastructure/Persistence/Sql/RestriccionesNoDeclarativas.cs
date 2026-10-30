using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Menus.Infrastructure.Persistence.Sql;

/// <summary>
/// Restricciones de integridad del #11 que EF Core no puede declarar en el modelo: los triggers de su §7
/// y el índice único sobre el nombre normalizado de alimentos y platos (R5-16).
/// </summary>
/// <remarks>
/// Cada restricción vive en un archivo <c>.sql</c> de esta carpeta, incrustado en el ensamblado, para que
/// se pueda revisar y probar aparte y no se pierda al regenerar una migración. La migración que crea el
/// esquema las aplica con una sola llamada: <see cref="AplicarRestriccionesNoDeclarativas"/> al final de
/// <c>Up()</c> y <see cref="QuitarRestriccionesNoDeclarativas"/> al principio de <c>Down()</c>.
/// Una restricción que llega con una migración posterior se aplica en esa migración, con
/// <see cref="AplicarScript"/>, y no en <c>InitialCreate</c>: así cada migración deja la base coherente
/// con su propio esquema.
/// Una base creada con <c>EnsureCreated()</c> no las tiene; para pruebas se puede ejecutar
/// <see cref="ScriptsDeCreacion"/>.
/// </remarks>
public static class RestriccionesNoDeclarativas
{
    private const string Prefijo = "Menus.Infrastructure.Persistence.Sql.";
    private const string ScriptQuitar = Prefijo + "Quitar.sql";

    /// <summary>
    /// Scripts que agrega una migración posterior a <c>InitialCreate</c>.
    /// <see cref="AplicarRestriccionesNoDeclarativas"/> no los aplica, por dos razones:
    /// <list type="bullet">
    /// <item>cada migración deja la base como la describe su propio esquema, también al bajar. Si <c>InitialCreate</c>
    /// aplicara un script posterior, el <c>Down()</c> de la migración que lo agrega lo quitaría, y la base volvería a
    /// un estado distinto del que crea <c>InitialCreate</c>;</item>
    /// <item>algunos leen columnas que esa migración crea: si <c>InitialCreate</c> los aplicara, una base detenida
    /// en ella tendría un trigger que lee una columna que todavía no existe.</item>
    /// </list>
    /// </summary>
    private static readonly HashSet<string> ScriptsPosteriores =
    [
        Prefijo + "RS_03_revisor_jefe.sql", // AgregarEsJefeANutricionista (columna es_jefe)
        Prefijo + "RS_07_parametrizacion_sin_altas.sql", // CerrarParametrizacionEItemsDelMenu
        Prefijo + "RS_15_items_de_menu_dictaminado.sql", // CerrarParametrizacionEItemsDelMenu
    ];

    /// <summary>Crea los triggers y el índice normalizado de <c>InitialCreate</c>. Va al final de <c>Up()</c>.</summary>
    /// <param name="migrationBuilder">El constructor de la migración.</param>
    public static void AplicarRestriccionesNoDeclarativas(this MigrationBuilder migrationBuilder)
    {
        foreach (var script in Scripts(incluirPosteriores: false))
        {
            migrationBuilder.Sql(script);
        }
    }

    /// <summary>Borra lo que crea <see cref="AplicarRestriccionesNoDeclarativas"/>. Va al principio de <c>Down()</c>.</summary>
    /// <remarks>Usa <c>DROP … IF EXISTS</c>, así que también borra lo que hayan agregado migraciones posteriores.</remarks>
    /// <param name="migrationBuilder">El constructor de la migración.</param>
    public static void QuitarRestriccionesNoDeclarativas(this MigrationBuilder migrationBuilder)
        => migrationBuilder.Sql(Leer(ScriptQuitar));

    /// <summary>
    /// Aplica un solo script de esta carpeta. Lo usa la migración posterior a <c>InitialCreate</c> que agrega
    /// la restricción.
    /// </summary>
    /// <remarks>
    /// El script tiene que figurar en <see cref="ScriptsPosteriores"/>, para que <c>InitialCreate</c> no lo
    /// aplique antes de que existan las columnas que usa. Conviene que sea idempotente
    /// (<c>CREATE OR REPLACE FUNCTION</c> y <c>DROP TRIGGER IF EXISTS</c>), porque las pruebas que crean la base
    /// con <c>EnsureCreated()</c> lo reciben también con <see cref="ScriptsDeCreacion"/>.
    /// </remarks>
    /// <param name="migrationBuilder">El constructor de la migración.</param>
    /// <param name="archivo">El nombre del archivo, por ejemplo <c>RS_03_revisor_jefe.sql</c>.</param>
    /// <exception cref="InvalidOperationException">Si el script no figura entre los de migraciones posteriores.</exception>
    public static void AplicarScript(this MigrationBuilder migrationBuilder, string archivo)
    {
        var recurso = Prefijo + archivo;
        if (!ScriptsPosteriores.Contains(recurso))
        {
            throw new InvalidOperationException(
                $"El script {archivo} no figura entre los de migraciones posteriores: InitialCreate ya lo aplica.");
        }

        migrationBuilder.Sql(Leer(recurso));
    }

    /// <summary>
    /// Todos los scripts de creación, incluidos los de migraciones posteriores, en el orden alfabético de sus
    /// archivos. Sirve para pruebas que crean la base con <c>EnsureCreated()</c>, que ya tiene todas las columnas.
    /// </summary>
    /// <returns>El contenido de cada script.</returns>
    public static IReadOnlyList<string> ScriptsDeCreacion() => Scripts(incluirPosteriores: true);

    private static List<string> Scripts(bool incluirPosteriores)
        => Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefijo, StringComparison.Ordinal)
                && n.EndsWith(".sql", StringComparison.Ordinal)
                && n != ScriptQuitar
                && (incluirPosteriores || !ScriptsPosteriores.Contains(n)))
            .Order(StringComparer.Ordinal)
            .Select(Leer)
            .ToList();

    private static Assembly Assembly => typeof(RestriccionesNoDeclarativas).Assembly;

    private static string Leer(string recurso)
    {
        using var stream = Assembly.GetManifestResourceStream(recurso)
            ?? throw new InvalidOperationException($"No se encontró el script {recurso}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
