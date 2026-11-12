using FluentValidation;

namespace Menus.Application.GestionDeMenus.RegistrarReemplazo;

/// <summary>Forma de la entrada de <see cref="RegistrarReemplazoCommand"/> (ADR-010, §3).</summary>
internal sealed class RegistrarReemplazoValidator : AbstractValidator<RegistrarReemplazoCommand>
{
    public RegistrarReemplazoValidator()
    {
        RuleFor(c => c.MenuRechazadoId).NotEmpty().WithMessage("El menú rechazado es obligatorio.");
    }
}
