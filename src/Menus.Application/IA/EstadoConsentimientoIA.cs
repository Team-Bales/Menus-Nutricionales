namespace Menus.Application.IA;

/// <summary>
/// Estado del consentimiento del paciente para el uso de sus datos clínicos en la IA.
/// </summary>
/// <remarks>
/// El valor por defecto (cuando no se ha registrado) es <see cref="NoRegistrado"/>, que se trata
/// igual que <see cref="No"/> a efectos de privacidad: las combinaciones raras se generalizan.
/// </remarks>
public enum EstadoConsentimientoIA
{
    /// <summary>
    /// No se ha registrado el consentimiento (valor por defecto). Equivale a <see cref="No"/>.
    /// </summary>
    NoRegistrado = 0,

    /// <summary>
    /// El paciente no dio su consentimiento.
    /// </summary>
    No = 1,

    /// <summary>
    /// El paciente dio su consentimiento.
    /// </summary>
    Si = 2
}
