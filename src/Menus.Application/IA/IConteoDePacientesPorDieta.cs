namespace Menus.Application.IA;

/// <summary>
/// Cuenta los pacientes que comparten una combinación de rango de edad y dieta, para la regla de combinaciones raras
/// del <see cref="TransformadorDePaciente"/> (contrato de IA, §5.2 y §5.3; #325).
/// </summary>
/// <remarks>
/// El transformador recibe el conteo ya calculado: «el cálculo del conteo queda para los proveedores» (§5.3). El
/// conteo es de pacientes distintos de todo el sistema, sin mirar el consentimiento. Es solo lectura, y la
/// implementación está en <c>Menus.Infrastructure</c>.
/// </remarks>
public interface IConteoDePacientesPorDieta
{
    /// <summary>Cuenta los pacientes inscritos en la dieta cuya edad está en el rango dado.</summary>
    /// <param name="nombreDieta">Nombre de la dieta, como llega al transformador.</param>
    /// <param name="edadMinima">Edad mínima del rango, incluida.</param>
    /// <param name="edadMaxima">Edad máxima del rango, incluida; <see langword="null"/> si el rango no tiene tope (65+).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La cantidad de pacientes distintos, 0 si no hay ninguno.</returns>
    Task<int> ContarAsync(string nombreDieta, int edadMinima, int? edadMaxima, CancellationToken ct);
}
