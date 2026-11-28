namespace Menus.Application.IA;

/// <summary>
/// Puerto para proveedores de bloques de contexto. Un proveedor por fuente, registrado por reflexión.
/// </summary>
/// <remarks>
/// Las implementaciones deben transformar los datos crudos aplicando las reglas de privacidad
/// (usando <see cref="TransformadorDePaciente"/> donde corresponda) y devolver un
/// <see cref="ResultadoDeContexto"/> con el bloque y las advertencias.
/// </remarks>
public interface IProveedorDeContexto
{
    /// <summary>
    /// Nombre de la fuente que provee este proveedor (p. ej., "valoracion", "aceptacion").
    /// </summary>
    string Fuente { get; }

    /// <summary>
    /// Construye el bloque de contexto para el caso dado.
    /// </summary>
    /// <param name="caso">Contexto del caso con los identificadores y pacientes necesarios.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>
    /// <see cref="ResultadoDeContexto"/> con el bloque y advertencias, o <c>null</c> si no hay datos para esta fuente.
    /// </returns>
    Task<ResultadoDeContexto?> ConstruirAsync(ContextoDeCaso caso, CancellationToken ct = default);
}
