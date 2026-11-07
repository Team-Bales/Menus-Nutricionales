using FluentValidation;

namespace Menus.Application.Catalogo.Dietas.ListarDietas;

/// <summary>Forma de la entrada de <see cref="ListarDietasQuery"/> (ADR-010, §3).</summary>
internal sealed class ListarDietasValidator : AbstractValidator<ListarDietasQuery>
{
    /// <summary>Tope de dietas por página, para que un cliente no pida el catálogo entero de una vez.</summary>
    internal const int TamanoMaximo = 100;

    public ListarDietasValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1).WithMessage("La página empieza en 1.");
        RuleFor(q => q.Size)
            .InclusiveBetween(1, TamanoMaximo).WithMessage($"El tamaño de página va de 1 a {TamanoMaximo}.");
    }
}
