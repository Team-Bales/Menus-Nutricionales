namespace Menus.Application.IA;

/// <summary>
/// Resultado de la construcción de un bloque de contexto, que incluye el bloque y las advertencias generadas.
/// </summary>
/// <remarks>
/// Copia defensivamente la lista de advertencias.
/// </remarks>
public sealed record ResultadoDeContexto
{
    /// <summary>
    /// Bloque de contexto construido. Puede ser <c>null</c> si no hay datos para esta fuente.
    /// </summary>
    public BloqueDeContexto? Bloque { get; }

    /// <summary>
    /// Advertencias generadas durante la construcción del bloque. Nunca <c>null</c> (se normaliza a lista vacía).
    /// </summary>
    public IReadOnlyList<string> Advertencias { get; }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ResultadoDeContexto"/>.
    /// </summary>
    /// <param name="bloque">Bloque de contexto construido. Puede ser <c>null</c>.</param>
    /// <param name="advertencias">Advertencias generadas. <c>null</c> se normaliza a lista vacía.</param>
    public ResultadoDeContexto(BloqueDeContexto? bloque, IReadOnlyList<string>? advertencias)
    {
        Bloque = bloque;
        Advertencias = (advertencias ?? Array.Empty<string>()).ToArray();
    }

    /// <summary>
    /// Devuelve una representación textual que no expone el contenido de las advertencias.
    /// </summary>
    /// <returns>Descripción genérica con conteos.</returns>
    public override string ToString()
        => $"ResultadoDeContexto {{ Bloque = {Bloque?.Fuente ?? "null"}, Advertencias = {Advertencias.Count} }}";
}
