using FluentValidation;
using Menus.Application.Comun;

namespace Menus.Application.GestionDeMenus.ListarMenus;

/// <summary>Forma de la entrada de <see cref="ListarMenusQuery"/> (ADR-010, §3).</summary>
internal sealed class ListarMenusValidator : AbstractValidator<ListarMenusQuery>
{
    public ListarMenusValidator()
    {
        RuleFor(q => q.DietaId).NotEmpty().WithMessage("La dieta es obligatoria.");
        RuleFor(q => q.Estado).IsInEnum().WithMessage("El estado es Pendiente, Final o Rechazado.");
        RuleFor(q => q.Page).Pagina();
        RuleFor(q => q.Size).TamanoDePagina();
    }
}
