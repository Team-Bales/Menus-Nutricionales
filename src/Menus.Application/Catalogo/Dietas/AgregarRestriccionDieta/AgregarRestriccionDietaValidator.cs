using FluentValidation;

namespace Menus.Application.Catalogo.Dietas.AgregarRestriccionDieta;

/// <summary>Forma de la entrada de <see cref="AgregarRestriccionDietaCommand"/> (ADR-010, §3).</summary>
internal sealed class AgregarRestriccionDietaValidator : AbstractValidator<AgregarRestriccionDietaCommand>
{
    public AgregarRestriccionDietaValidator()
    {
        RuleFor(c => c.RestriccionAlimentariaId).NotEmpty().WithMessage("La restricción alimentaria es obligatoria.");
    }
}
