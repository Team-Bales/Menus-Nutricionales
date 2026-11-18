using FluentValidation;

namespace Menus.Application.Nutricionistas.RevocarDieta;

/// <summary>Forma de la entrada de <see cref="RevocarDietaCommand"/> (ADR-010, §3).</summary>
internal sealed class RevocarDietaValidator : AbstractValidator<RevocarDietaCommand>
{
    public RevocarDietaValidator()
    {
        RuleFor(c => c.DietaId).NotEmpty().WithMessage("La dieta es obligatoria.");
    }
}
