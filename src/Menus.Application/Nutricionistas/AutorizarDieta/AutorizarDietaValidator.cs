using FluentValidation;

namespace Menus.Application.Nutricionistas.AutorizarDieta;

/// <summary>Forma de la entrada de <see cref="AutorizarDietaCommand"/> (ADR-010, §3).</summary>
internal sealed class AutorizarDietaValidator : AbstractValidator<AutorizarDietaCommand>
{
    public AutorizarDietaValidator()
    {
        RuleFor(c => c.DietaId).NotEmpty().WithMessage("La dieta es obligatoria.");
    }
}
