using Microsoft.EntityFrameworkCore.Migrations;

namespace Menus.Infrastructure.Persistence.Consultas;

/// <summary>
/// Las consultas informacionales del informe (§2.2) como funciones de PostgreSQL, una por consulta o por parte de
/// consulta. Cada una sigue al pie de la letra su reformulación y su trayectoria (§5.5).
/// </summary>
/// <remarks>
/// <para>
/// Viven en archivos <c>.sql</c> de esta carpeta, incrustados en el ensamblado, igual que las restricciones de
/// <c>Persistence/Sql</c>: se revisan y se prueban aparte, y regenerar una migración no las borra.
/// </para>
/// <para>
/// Son funciones y no vistas porque reciben parámetros (la dieta, el revisor, el menú, el período). Sus nombres
/// empiezan por <c>consulta_</c> y no por <c>fn_</c>, para no mezclarse con las funciones de los triggers.
/// </para>
/// <para>
/// Cada migración aplica por su nombre los scripts que agrega, con <see cref="AplicarConsulta"/>. No se aplican
/// todos los de la carpeta, porque se leen al ejecutar la migración: una consulta agregada después también le
/// llegaría a una migración anterior.
/// </para>
/// </remarks>
public static class ConsultasInformacionales
{
    private const string Prefijo = "Menus.Infrastructure.Persistence.Consultas.";

    /// <summary>Aplica un script de esta carpeta. Los scripts usan <c>CREATE OR REPLACE FUNCTION</c>.</summary>
    /// <param name="migrationBuilder">El constructor de la migración.</param>
    /// <param name="archivo">El nombre del archivo, por ejemplo <c>Consulta_2_alimentos_mas_usados.sql</c>.</param>
    /// <exception cref="InvalidOperationException">Si el script no existe en el ensamblado.</exception>
    public static void AplicarConsulta(this MigrationBuilder migrationBuilder, string archivo)
        => migrationBuilder.Sql(Leer(archivo));

    /// <summary>Devuelve el contenido de un script de esta carpeta.</summary>
    /// <param name="archivo">El nombre del archivo.</param>
    /// <returns>El SQL del script.</returns>
    /// <exception cref="InvalidOperationException">Si el script no existe en el ensamblado.</exception>
    public static string Leer(string archivo)
    {
        var recurso = Prefijo + archivo;
        using var stream = typeof(ConsultasInformacionales).Assembly.GetManifestResourceStream(recurso)
            ?? throw new InvalidOperationException($"No se encontró el script {recurso}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
