using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Menus.Domain.Excepciones;

/// <summary>
/// Un dato de la operación no es válido, sea cual sea el estado del agregado: un nombre vacío, una cantidad no
/// positiva, un identificador que no es del catálogo. La API la traduce a 422 (ADR-010, §4).
/// </summary>
public sealed class DatoInvalidoException : ExcepcionDeDominio
{
    /// <param name="campo">Nombre del parámetro inválido, para asociar el error al campo de la entrada.</param>
    /// <param name="mensaje">Descripción del problema, pensada para mostrarse al usuario.</param>
    /// <param name="regla">Código de la regla del informe (por ejemplo «RD-05»), si se conoce.</param>
    public DatoInvalidoException(string campo, string mensaje, string? regla = null)
        : base(mensaje, regla)
    {
        Campo = campo;
    }

    /// <summary>
    /// Nombre del parámetro inválido.
    /// </summary>
    public string Campo { get; }

    /// <summary>
    /// Lanza la excepción si <paramref name="valor"/> es nulo, vacío o solo espacios. Reemplaza en el dominio a
    /// <see cref="ArgumentException.ThrowIfNullOrWhiteSpace"/>.
    /// </summary>
    /// <param name="valor">Texto a comprobar.</param>
    /// <param name="campo">Nombre del parámetro; lo completa el compilador.</param>
    /// <exception cref="DatoInvalidoException">Si <paramref name="valor"/> es nulo, vacío o solo espacios.</exception>
    public static void SiVacio([NotNull] string? valor, [CallerArgumentExpression(nameof(valor))] string campo = "")
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DatoInvalidoException(campo, $"El campo {campo} no puede estar vacío.");
        }
    }
}
