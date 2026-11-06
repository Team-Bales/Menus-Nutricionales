namespace Menus.Domain.Excepciones;

/// <summary>
/// La operación viola una regla del agregado en su estado actual: por ejemplo, quitar el único grupo requerido de
/// una dieta (RK-07) o dictaminar un menú que ya tiene dictamen. La API la traduce a 409 (ADR-010, §4).
/// </summary>
public sealed class ReglaDeNegocioException : ExcepcionDeDominio
{
    /// <param name="mensaje">Descripción de la regla violada, pensada para mostrarse al usuario.</param>
    /// <param name="regla">Código de la regla del informe (por ejemplo «RK-07»), si se conoce.</param>
    public ReglaDeNegocioException(string mensaje, string? regla = null)
        : base(mensaje, regla)
    {
    }
}
