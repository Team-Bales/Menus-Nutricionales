using FluentValidation;
using Menus.Application.Comun;

namespace Menus.Application.Nutricionistas.ListarNutricionistas;

/// <summary>Forma de la entrada de <see cref="ListarNutricionistasQuery"/> (ADR-010, §3).</summary>
internal sealed class ListarNutricionistasValidator : AbstractValidator<ListarNutricionistasQuery>
{
    public ListarNutricionistasValidator()
    {
        RuleFor(q => q.Page).Pagina();
        RuleFor(q => q.Size).TamanoDePagina();
    }
}
