namespace Menus.Application.IA;

/// <summary>
/// Opciones de configuración para la integración de IA.
/// Sección de configuración: "IA".
/// </summary>
/// <remarks>
/// Clase (no record) con setters públicos para binding futuro.
/// La representación textual no expone <see cref="ApiKey"/>.
/// </remarks>
public sealed class IAOptions
{
    /// <summary>
    /// Nombre de la sección de configuración en appsettings.json.
    /// </summary>
    public const string SectionName = "IA";

    /// <summary>
    /// Indica si la integración de IA está habilitada. Por defecto <c>false</c>.
    /// </summary>
    public bool Habilitada { get; set; } = false;

    /// <summary>
    /// URL base del proveedor compatible con OpenAI. Por defecto "http://localhost:11434/v1" (Ollama local).
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434/v1";

    /// <summary>
    /// Nombre del modelo a usar. Por defecto cadena vacía; se fijará al probar el proveedor.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Clave de API del proveedor. Por defecto cadena vacía; se debe proveer via user-secrets o variable de entorno.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Timeout en segundos para las llamadas al proveedor. Por defecto 30.
    /// </summary>
    public int TimeoutSegundos { get; set; } = 30;

    /// <summary>
    /// Máximo número de bloques de contexto a enviar. Por defecto 4.
    /// </summary>
    public int MaxBloques { get; set; } = 4;

    /// <summary>
    /// Máximo de caracteres de contexto a enviar. Por defecto 6000.
    /// </summary>
    public int MaxCaracteresDeContexto { get; set; } = 6000;

    /// <summary>
    /// Versión del prompt para control de caché. Por defecto "v1".
    /// </summary>
    public string VersionDePrompt { get; set; } = "v1";

    /// <summary>
    /// Valor mínimo K para generalizar combinaciones raras sin consentimiento. Por defecto 5.
    /// </summary>
    public int KMinimo { get; set; } = 5;

    /// <summary>
    /// Confirmaciones exigidas antes de habilitar un proveedor de IA externo.
    /// No se requieren para Ollama local ni cuando <see cref="Habilitada"/> es <see langword="false"/>.
    /// </summary>
    public TerminosIAOptions Terminos { get; set; } = new();

    /// <summary>
    /// Devuelve una representación textual que no expone <see cref="ApiKey"/>.
    /// </summary>
    /// <returns>Descripción con valores seguros.</returns>
    public override string ToString()
        => $"IAOptions {{ Habilitada = {Habilitada}, BaseUrl = {BaseUrl}, Model = {Model}, ApiKey = [redacted], TimeoutSegundos = {TimeoutSegundos}, MaxBloques = {MaxBloques}, MaxCaracteresDeContexto = {MaxCaracteresDeContexto}, VersionDePrompt = {VersionDePrompt}, KMinimo = {KMinimo} }}";
}

/// <summary>Constancia de aceptación de los términos para un proveedor de IA externo.</summary>
public sealed class TerminosIAOptions
{
    /// <summary>Versión de los términos aceptados.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Persona responsable que aceptó los términos.</summary>
    public string AceptadoPor { get; set; } = string.Empty;

    /// <summary>Fecha de la aceptación.</summary>
    public DateOnly? Fecha { get; set; }

    /// <summary>Indica que se desactivaron la retención y el entrenamiento en el proveedor.</summary>
    public bool RetencionDesactivada { get; set; }
}
