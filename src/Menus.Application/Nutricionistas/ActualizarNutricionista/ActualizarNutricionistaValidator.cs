using FluentValidation;

namespace Menus.Application.Nutricionistas.ActualizarNutricionista;

/// <summary>Forma de la entrada de <see cref="ActualizarNutricionistaCommand"/> (ADR-010, §3).</summary>
internal sealed class ActualizarNutricionistaValidator : AbstractValidator<ActualizarNutricionistaCommand>
{
    public ActualizarNutricionistaValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

        RuleFor(c => c.EspecialidadId)
            .NotEmpty().WithMessage("La especialidad es obligatoria.");
    }
}
