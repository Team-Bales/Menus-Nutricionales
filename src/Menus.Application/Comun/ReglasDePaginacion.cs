using FluentValidation;

namespace Menus.Application.Comun;

/// <summary>Reglas de forma de la paginación de los listados (<see cref="ResultadoPaginado{T}"/>).</summary>
internal static class ReglasDePaginacion
{
    /// <summary>Tope de elementos por página, para que un cliente no pida un listado entero de una vez.</summary>
    internal const int TamanoMaximo = 100;

    /// <summary>La página empieza en 1.</summary>
    public static IRuleBuilderOptions<T, int> Pagina<T>(this IRuleBuilder<T, int> regla)
        => regla.GreaterThanOrEqualTo(1).WithMessage("La página empieza en 1.");

    /// <summary>El tamaño de página va de 1 a <see cref="TamanoMaximo"/>.</summary>
    public static IRuleBuilderOptions<T, int> TamanoDePagina<T>(this IRuleBuilder<T, int> regla)
        => regla.InclusiveBetween(1, TamanoMaximo).WithMessage($"El tamaño de página va de 1 a {TamanoMaximo}.");
}
