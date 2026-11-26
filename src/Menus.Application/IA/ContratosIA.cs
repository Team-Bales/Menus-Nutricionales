namespace Menus.Application.IA;

using System.Text.Json.Serialization;

/// <summary>
/// Tipo de tarea que se solicita a la IA. Valores explícitos y estables para serialización.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TareaIA
{
    /// <summary>
    /// Explicar en lenguaje natural un análisis (valoración, reporte, consumo).
    /// </summary>
    ExplicarAnalisis = 0,

    /// <summary>
    /// Borradores de recomendación para el nutricionista.
    /// </summary>
    SugerirAcciones = 1,

    /// <summary>
    /// Guion para que un nutricionista novato explique el caso a un paciente sin formación médica.
    /// </summary>
    GuionParaPaciente = 2
}

/// <summary>
/// Sección de la respuesta estructurada de la IA.
/// </summary>
/// <remarks>
/// <see cref="Titulo"/> y <see cref="Texto"/> no pueden ser nulos. La representación textual
/// no vuelca el contenido de la respuesta.
/// </remarks>
public sealed record SeccionDto
{
    /// <summary>
    /// Título de la sección. No nulo ni vacío.
    /// </summary>
    [JsonPropertyName("titulo")]
    public string Titulo { get; }

    /// <summary>
    /// Texto de la sección. No nulo ni vacío.
    /// </summary>
    [JsonPropertyName("texto")]
    public string Texto { get; }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SeccionDto"/>.
    /// </summary>
    /// <param name="Titulo">Título de la sección. No puede ser nulo.</param>
    /// <param name="Texto">Texto de la sección. No puede ser nulo.</param>
    /// <exception cref="ArgumentNullException"><paramref name="Titulo"/> o <paramref name="Texto"/> es nulo.</exception>
    [JsonConstructor]
    public SeccionDto(string Titulo, string Texto)
    {
        ArgumentNullException.ThrowIfNull(Titulo);
        ArgumentNullException.ThrowIfNull(Texto);

        this.Titulo = Titulo;
        this.Texto = Texto;
    }

    /// <summary>
    /// Devuelve una representación textual que no expone el contenido de la sección.
    /// </summary>
    /// <returns>Descripción genérica sin el contenido de <see cref="Titulo"/> ni <see cref="Texto"/>.</returns>
    public override string ToString() => "SeccionDto { Titulo = [redacted], Texto = [redacted] }";
}

/// <summary>
/// Respuesta estructurada devuelta por la IA.
/// </summary>
/// <remarks>
/// Todas las listas nunca son <c>null</c> (se normalizan a colecciones vacías).
/// Los textos obligatorios (<see cref="Resumen"/>, <see cref="GeneradoPorIA"/>) no pueden ser nulos.
/// La representación textual no vuelca el contenido de la respuesta.
/// </remarks>
public sealed record RespuestaIADto
{
    /// <summary>
    /// Resumen ejecutivo de la respuesta. No nulo ni vacío.
    /// </summary>
    [JsonPropertyName("resumen")]
    public string Resumen { get; }

    /// <summary>
    /// Secciones de la respuesta (pasos del guion en <see cref="TareaIA.GuionParaPaciente"/>).
    /// Nunca <c>null</c>; lista vacía si no hay secciones.
    /// </summary>
    [JsonPropertyName("secciones")]
    public IReadOnlyList<SeccionDto> Secciones { get; }

    /// <summary>
    /// Recomendaciones para el nutricionista (en <see cref="TareaIA.SugerirAcciones"/>).
    /// Nunca <c>null</c>; lista vacía si no hay recomendaciones.
    /// </summary>
    [JsonPropertyName("recomendaciones")]
    public IReadOnlyList<string> Recomendaciones { get; }

    /// <summary>
    /// Advertencias generadas por la IA o por el proceso de anonimización.
    /// Nunca <c>null</c>; lista vacía si no hay advertencias.
    /// </summary>
    [JsonPropertyName("advertencias")]
    public IReadOnlyList<string> Advertencias { get; }

    /// <summary>
    /// Fuentes usadas para construir la respuesta (bloques de contexto).
    /// Nunca <c>null</c>; lista vacía si no hay fuentes.
    /// </summary>
    [JsonPropertyName("fuentesUsadas")]
    public IReadOnlyList<string> FuentesUsadas { get; }

    /// <summary>
    /// Indica si el texto fue generado por IA (<c>true</c>) o es una plantilla determinista (<c>false</c>).
    /// </summary>
    [JsonPropertyName("generadoPorIA")]
    public bool GeneradoPorIA { get; }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="RespuestaIADto"/>.
    /// </summary>
    /// <param name="Resumen">Resumen ejecutivo. No puede ser nulo.</param>
    /// <param name="Secciones">Secciones de la respuesta. <c>null</c> se normaliza a lista vacía.</param>
    /// <param name="Recomendaciones">Recomendaciones. <c>null</c> se normaliza a lista vacía.</param>
    /// <param name="Advertencias">Advertencias. <c>null</c> se normaliza a lista vacía.</param>
    /// <param name="FuentesUsadas">Fuentes usadas. <c>null</c> se normaliza a lista vacía.</param>
    /// <param name="GeneradoPorIA">Indica si fue generado por IA.</param>
    /// <exception cref="ArgumentNullException"><paramref name="Resumen"/> es nulo.</exception>
    [JsonConstructor]
    public RespuestaIADto(
        string Resumen,
        IReadOnlyList<SeccionDto>? Secciones,
        IReadOnlyList<string>? Recomendaciones,
        IReadOnlyList<string>? Advertencias,
        IReadOnlyList<string>? FuentesUsadas,
        bool GeneradoPorIA)
    {
        ArgumentNullException.ThrowIfNull(Resumen);

        this.Resumen = Resumen;
        this.Secciones = (Secciones ?? Array.Empty<SeccionDto>()).ToArray();
        this.Recomendaciones = (Recomendaciones ?? Array.Empty<string>()).ToArray();
        this.Advertencias = (Advertencias ?? Array.Empty<string>()).ToArray();
        this.FuentesUsadas = (FuentesUsadas ?? Array.Empty<string>()).ToArray();
        this.GeneradoPorIA = GeneradoPorIA;
    }

    /// <summary>
    /// Devuelve una representación textual que no expone el contenido de la respuesta.
    /// </summary>
    /// <returns>Descripción genérica con conteos pero sin contenido.</returns>
    public override string ToString()
        => $"RespuestaIADto {{ Resumen = [redacted], Secciones = {Secciones.Count}, Recomendaciones = {Recomendaciones.Count}, Advertencias = {Advertencias.Count}, FuentesUsadas = {FuentesUsadas.Count}, GeneradoPorIA = {GeneradoPorIA} }}";
}




