namespace Menus.Application.IA;

using System.Collections.Frozen;

/// <summary>
/// Bloque de contexto inmutable que encapsula una fuente de datos transformada para la IA.
/// </summary>
/// <remarks>
/// Conserva exactamente cuatro campos: <see cref="Fuente"/>, <see cref="Version"/>, <see cref="Resumen"/> y <see cref="Cifras"/>.
/// Rechaza fuente vacía, versión < 1 y cifras NaN/Infinity. Copia defensivamente <see cref="Cifras"/>.
/// </remarks>
public sealed record BloqueDeContexto
{
    /// <summary>
    /// Nombre de la fuente (p. ej., "valoracion", "aceptacion", "reporte-1", "ficha").
    /// No puede ser nulo ni solo espacios.
    /// </summary>
    public string Fuente { get; }

    /// <summary>
    /// Versión del esquema del bloque. Debe ser >= 1.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Resumen compacto en texto, la que va al prompt. No puede ser nulo.
    /// </summary>
    public string Resumen { get; }

    /// <summary>
    /// Cifras que contiene el bloque, para verificación posterior. Nunca <c>null</c> (se normaliza a diccionario vacío).
    /// Claves no nulas; valores finitos (ni NaN ni Infinity).
    /// </summary>
    public IReadOnlyDictionary<string, double> Cifras { get; }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="BloqueDeContexto"/>.
    /// </summary>
    /// <param name="fuente">Nombre de la fuente. No puede ser nulo ni solo espacios.</param>
    /// <param name="version">Versión del esquema. Debe ser >= 1.</param>
    /// <param name="resumen">Resumen compacto. No puede ser nulo.</param>
    /// <param name="cifras">Cifras del bloque. <c>null</c> se normaliza a diccionario vacío. Valores deben ser finitos.</param>
    /// <exception cref="ArgumentException"><paramref name="fuente"/> es nulo o solo espacios; <paramref name="version"/> < 1; o <paramref name="cifras"/> contiene NaN/Infinity.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="resumen"/> es nulo.</exception>
    public BloqueDeContexto(string fuente, int version, string resumen, IReadOnlyDictionary<string, double>? cifras)
    {
        if (string.IsNullOrWhiteSpace(fuente))
        {
            throw new ArgumentException("La fuente no puede ser nula ni solo espacios.", nameof(fuente));
        }

        if (version < 1)
        {
            throw new ArgumentException("La versión debe ser >= 1.", nameof(version));
        }

        ArgumentNullException.ThrowIfNull(resumen);

        // Copia defensiva y validación de cifras
        var dict = new Dictionary<string, double>();
        if (cifras != null)
        {
            foreach (var kvp in cifras)
            {
                if (kvp.Key == null)
                {
                    throw new ArgumentException("Las claves de las cifras no pueden ser nulas.", nameof(cifras));
                }
                if (!double.IsFinite(kvp.Value))
                {
                    throw new ArgumentException($"El valor de la cifra '{kvp.Key}' no es finito (NaN/Infinity).", nameof(cifras));
                }
                dict[kvp.Key] = kvp.Value;
            }
        }

        Fuente = fuente;
        Version = version;
        Resumen = resumen;
        Cifras = dict.ToFrozenDictionary();
    }

    /// <summary>
    /// Devuelve una representación textual que no expone el contenido de <see cref="Resumen"/> ni <see cref="Cifras"/>.
    /// </summary>
    /// <returns>Descripción genérica con conteos pero sin contenido sensible.</returns>
    public override string ToString()
        => $"BloqueDeContexto {{ Fuente = {Fuente}, Version = {Version}, Resumen = [redacted], Cifras = {Cifras.Count} }}";
}
