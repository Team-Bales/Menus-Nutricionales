namespace Menus.Application.IA;

using System.Collections.Frozen;
using System.Text.Json.Serialization;

/// <summary>
/// Contexto opaco que identifica el caso y proporciona los pacientes para los proveedores de contexto.
/// </summary>
/// <remarks>
/// Tipo público porque aparece en la interfaz <see cref="IProveedorDeContexto"/>, pero constructor y
/// propiedades identificadoras son <c>internal</c>. Lleva los ids opcionales del caso, la colección
/// completa de pacientes de la petición y una <see cref="FechaReferencia"/>.
/// Construye una sola vez el mapa opaco ordenando los <see cref="Guid"/> distintos de forma estable.
/// Con un paciente devuelve la referencia "el paciente"; con varios, "P1", "P2"... coherentes para todos los proveedores.
/// Los ids, los pacientes y el mapa llevan <see cref="JsonIgnoreAttribute"/> (y son <c>internal</c>);
/// <see cref="ToString"/> no muestra valores.
/// El mapa nunca forma parte de ningún resultado serializable.
/// </remarks>
public sealed class ContextoDeCaso
{
    /// <summary>
    /// Identificador de la valoración (opcional).
    /// </summary>
    [JsonIgnore]
    internal Guid? ValoracionId { get; }

    /// <summary>
    /// Identificador del paciente principal (opcional).
    /// </summary>
    [JsonIgnore]
    internal Guid? PacienteId { get; }

    /// <summary>
    /// Identificador del menú (opcional).
    /// </summary>
    [JsonIgnore]
    internal Guid? MenuId { get; }

    /// <summary>
    /// Identificador de la dieta (opcional).
    /// </summary>
    [JsonIgnore]
    internal Guid? DietaId { get; }

    /// <summary>
    /// Colección completa de pacientes de la petición (solo lectura). Interna y fuera del JSON: los snapshots
    /// llevan id, nombre, edad exacta y sala, que no deben salir del servidor.
    /// </summary>
    [JsonIgnore]
    internal IReadOnlyList<PacienteSnapshot> Pacientes { get; }

    /// <summary>
    /// Fecha de referencia para expresar fechas relativas.
    /// </summary>
    public DateOnly FechaReferencia { get; }

    /// <summary>
    /// Mapa opaco de <see cref="Guid"/> de paciente a código estable ("el paciente", "P1", "P2", ...).
    /// Nunca <c>null</c>; se construye una sola vez en el constructor.
    /// </summary>
    [JsonIgnore]
    internal IReadOnlyDictionary<Guid, string> MapaCodigosPaciente { get; }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ContextoDeCaso"/>.
    /// </summary>
    /// <param name="pacientes">Colección completa de pacientes de la petición. No puede ser <c>null</c> ni vacía.</param>
    /// <param name="fechaReferencia">Fecha de referencia para fechas relativas.</param>
    /// <param name="valoracionId">Identificador de la valoración (opcional).</param>
    /// <param name="pacienteId">Identificador del paciente principal (opcional).</param>
    /// <param name="menuId">Identificador del menú (opcional).</param>
    /// <param name="dietaId">Identificador de la dieta (opcional).</param>
    /// <exception cref="ArgumentException"><paramref name="pacientes"/> es <c>null</c> o vacía.</exception>
    internal ContextoDeCaso(
        IReadOnlyList<PacienteSnapshot> pacientes,
        DateOnly fechaReferencia,
        Guid? valoracionId = null,
        Guid? pacienteId = null,
        Guid? menuId = null,
        Guid? dietaId = null)
    {
        if (pacientes == null || pacientes.Count == 0)
        {
            throw new ArgumentException("La colección de pacientes no puede ser nula ni vacía.", nameof(pacientes));
        }

        Pacientes = pacientes;
        FechaReferencia = fechaReferencia;
        ValoracionId = valoracionId;
        PacienteId = pacienteId;
        MenuId = menuId;
        DietaId = dietaId;

        // Construir mapa opaco ordenando GUIDs de forma estable
        var guidsDistintos = pacientes
            .Select(p => p.Id)
            .Distinct()
            .OrderBy(g => g)
            .ToArray();

        var mapa = new Dictionary<Guid, string>(guidsDistintos.Length);
        if (guidsDistintos.Length == 1)
        {
            mapa[guidsDistintos[0]] = "el paciente";
        }
        else
        {
            for (int i = 0; i < guidsDistintos.Length; i++)
            {
                mapa[guidsDistintos[i]] = $"P{i + 1}";
            }
        }

        MapaCodigosPaciente = mapa.ToFrozenDictionary();
    }

    /// <summary>
    /// Obtiene el código opaco para un paciente dado.
    /// </summary>
    /// <param name="pacienteId">Identificador del paciente.</param>
    /// <returns>Código opaco ("el paciente", "P1", "P2", ...) o <c>null</c> si el paciente no está en el contexto.</returns>
    internal string? ObtenerCodigoPaciente(Guid pacienteId)
        => MapaCodigosPaciente.TryGetValue(pacienteId, out var codigo) ? codigo : null;

    /// <summary>
    /// Devuelve una representación textual que no expone valores sensibles.
    /// </summary>
    /// <returns>Descripción genérica sin IDs ni mapa.</returns>
    public override string ToString()
        => $"ContextoDeCaso {{ Pacientes = {Pacientes.Count}, FechaReferencia = {FechaReferencia:yyyy-MM-dd} }}";
}

/// <summary>
/// Snapshot interno de un paciente para el transformador.
/// </summary>
/// <remarks>
/// No existe campo de texto libre. Incluye id, nombre, edad, sala, fechas exactas, consentimiento y datos clínicos tipados.
/// </remarks>
public sealed record PacienteSnapshot
{
    /// <summary>
    /// Identificador único del paciente.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Nombre del paciente (no vacío).
    /// </summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// Edad en años enteros (>= 0).
    /// </summary>
    public int Edad { get; init; }

    /// <summary>
    /// Identificador de la sala de hospitalización.
    /// </summary>
    public Guid SalaId { get; init; }

    /// <summary>
    /// Fecha de ingreso (opcional).
    /// </summary>
    public DateOnly? FechaIngreso { get; init; }

    /// <summary>
    /// Fecha de alta estimada (opcional).
    /// </summary>
    public DateOnly? FechaAltaEstimada { get; init; }

    /// <summary>
    /// Estado de consentimiento para IA.
    /// </summary>
    public EstadoConsentimientoIA ConsentimientoIA { get; init; } = EstadoConsentimientoIA.NoRegistrado;

    /// <summary>
    /// Dietas del paciente (clave = nombre de la dieta, valor = tipo clínico).
    /// </summary>
    public IReadOnlyDictionary<string, string> Dietas { get; init; } = FrozenDictionary<string, string>.Empty;

    /// <summary>
    /// Alérgenos del paciente (clave = nombre del alérgeno, valor = tipo clínico).
    /// </summary>
    public IReadOnlyDictionary<string, string> Alergenos { get; init; } = FrozenDictionary<string, string>.Empty;

    /// <summary>
    /// Devuelve una representación textual que no expone nombre, ids, edad, sala, fechas ni datos clínicos
    /// (el <c>ToString</c> autogenerado de un record los imprime todos).
    /// </summary>
    /// <returns>Descripción genérica con solo los conteos.</returns>
    public override string ToString()
        => $"PacienteSnapshot {{ Dietas = {Dietas.Count}, Alergenos = {Alergenos.Count} }}";
}
