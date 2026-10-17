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
/// Una base creada con <c>EnsureCreated()</c> no las tiene; para pruebas se puede ejecutar
/// <see cref="ScriptsDeCreacion"/>.
/// </remarks>
public static class RestriccionesNoDeclarativas
{
    private const string Prefijo = "Menus.Infrastructure.Persistence.Sql.";
    private const string ScriptQuitar = Prefijo + "Quitar.sql";

    /// <summary>Crea los triggers y el índice normalizado. Va al final de <c>Up()</c>.</summary>
    /// <param name="migrationBuilder">El constructor de la migración.</param>
    public static void AplicarRestriccionesNoDeclarativas(this MigrationBuilder migrationBuilder)
    {
        foreach (var script in ScriptsDeCreacion())
        {
            migrationBuilder.Sql(script);
        }
    }

    /// <summary>Borra lo que crea <see cref="AplicarRestriccionesNoDeclarativas"/>. Va al principio de <c>Down()</c>.</summary>
    /// <param name="migrationBuilder">El constructor de la migración.</param>
    public static void QuitarRestriccionesNoDeclarativas(this MigrationBuilder migrationBuilder)
        => migrationBuilder.Sql(Leer(ScriptQuitar));

    /// <summary>Los scripts de creación, en el orden alfabético de sus archivos.</summary>
    /// <returns>El contenido de cada script.</returns>
    public static IReadOnlyList<string> ScriptsDeCreacion()
        => Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefijo, StringComparison.Ordinal)
                && n.EndsWith(".sql", StringComparison.Ordinal)
                && n != ScriptQuitar)
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
