namespace Menus.Domain.Excepciones;

/// <summary>
/// Base de las excepciones que lanza el dominio cuando la operación pedida viola una regla o recibe un dato
/// inválido (ADR-010, §4).
/// </summary>
/// <remarks>
/// Se separan de <see cref="InvalidOperationException"/> y <see cref="ArgumentException"/> porque esas también las
/// lanza el framework ante un error de programación. Con excepciones propias, la API traduce a 409 o 422 solo lo que
/// viene del dominio, y cualquier otro error sigue siendo un 500.
/// </remarks>
public abstract class ExcepcionDeDominio : Exception
{
    /// <param name="mensaje">Descripción del problema, pensada para mostrarse al usuario.</param>
    /// <param name="regla">Código de la regla del informe que se viola (por ejemplo «RK-07»), si se conoce.</param>
    protected ExcepcionDeDominio(string mensaje, string? regla)
        : base(mensaje)
    {
        Regla = regla;
    }

    /// <summary>
    /// Código de la regla del informe que se viola (por ejemplo «RK-07»), o <see langword="null"/> si no tiene.
    /// </summary>
    public string? Regla { get; }
}
