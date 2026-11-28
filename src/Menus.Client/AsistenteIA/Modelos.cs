namespace Menus.Client.AsistenteIA;

/// <summary>Tarea que el nutricionista solicita al asistente, según el contrato de IA.</summary>
public enum TareaDeIA
{
    /// <summary>Explica una valoración o análisis en lenguaje natural.</summary>
    ExplicarAnalisis,

    /// <summary>Propone borradores de recomendaciones para el nutricionista.</summary>
    SugerirAcciones,

    /// <summary>Prepara un guion en lenguaje claro para el paciente.</summary>
    GuionParaPaciente,
}

/// <summary>Solicitud que la pestaña de IA envía para una valoración concreta.</summary>
/// <param name="Tarea">Tipo de asistencia solicitado.</param>
/// <param name="ValoracionId">Identificador de la valoración que aporta el caso.</param>
/// <param name="Bloques">Fuentes de contexto seleccionadas por el nutricionista.</param>
/// <param name="Pregunta">Pregunta libre opcional, no permitida para <see cref="TareaDeIA.GuionParaPaciente"/>.</param>
public sealed record SolicitudDeIA(TareaDeIA Tarea, Guid ValoracionId, IReadOnlyList<string> Bloques, string? Pregunta);

/// <summary>Sección con título y texto de una respuesta del asistente.</summary>
/// <param name="Titulo">Título visible de la sección.</param>
/// <param name="Texto">Contenido de la sección.</param>
public sealed record SeccionDeIA(string Titulo, string Texto);

/// <summary>Respuesta segura que devuelve el contrato de IA a la interfaz.</summary>
/// <param name="Resumen">Resumen ejecutivo de la respuesta.</param>
/// <param name="Secciones">Secciones o pasos del guion.</param>
/// <param name="Recomendaciones">Borradores de recomendación.</param>
/// <param name="Advertencias">Limitaciones o advertencias del resultado.</param>
/// <param name="FuentesUsadas">Bloques de contexto que efectivamente se usaron.</param>
/// <param name="GeneradoPorIA">Indica si el texto procede del modelo o de una plantilla determinista.</param>
public sealed record RespuestaDeIA(
    string Resumen,
    IReadOnlyList<SeccionDeIA> Secciones,
    IReadOnlyList<string> Recomendaciones,
    IReadOnlyList<string> Advertencias,
    IReadOnlyList<string> FuentesUsadas,
    bool GeneradoPorIA);
