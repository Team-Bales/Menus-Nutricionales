namespace Menus.Domain.ValueObjects;

/// <summary>
/// Cantidad total de alimentos de un menú generado.
/// Valor positivo (RD-10, RS-08).
/// </summary>
/// <param name="Valor">Número de alimentos que contendrá el menú.</param>
public sealed record CantidadTotalAlimentos(int Valor)
{
    /// <summary>
    /// Crea una nueva cantidad total de alimentos.
    /// </summary>
    /// <param name="valor">Número de alimentos (> 0).</param>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="valor"/> no es estrictamente positivo.</exception>
    public static CantidadTotalAlimentos Crear(int valor)
    {
        if (valor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "La cantidad total de alimentos debe ser estrictamente positiva.");
        }
        return new CantidadTotalAlimentos(valor);
    }

    /// <summary>
    /// Conversión implícita a <see cref="int"/>.
    /// </summary>
    public static implicit operator int(CantidadTotalAlimentos cantidad) => cantidad.Valor;

    /// <summary>
    /// Conversión explícita desde <see cref="int"/>.
    /// </summary>
    public static explicit operator CantidadTotalAlimentos(int valor) => Crear(valor);
}
