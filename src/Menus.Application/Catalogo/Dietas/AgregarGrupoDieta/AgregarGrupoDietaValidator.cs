using FluentValidation;

namespace Menus.Application.Catalogo.Dietas.AgregarGrupoDieta;

/// <summary>Forma de la entrada de <see cref="AgregarGrupoDietaCommand"/> (ADR-010, §3).</summary>
internal sealed class AgregarGrupoDietaValidator : AbstractValidator<AgregarGrupoDietaCommand>
{
    public AgregarGrupoDietaValidator()
    {
        RuleFor(c => c.GrupoNutricionalId).NotEmpty().WithMessage("El grupo nutricional es obligatorio.");
        RuleFor(c => c.Tipo).IsInEnum().WithMessage("'{PropertyValue}' no es un tipo de cobertura válido.");
    }
}
