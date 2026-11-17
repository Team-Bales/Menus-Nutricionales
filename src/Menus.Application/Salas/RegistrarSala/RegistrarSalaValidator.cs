using FluentValidation;

namespace Menus.Application.Salas.RegistrarSala;

/// <summary>Forma de la entrada de <see cref="RegistrarSalaCommand"/> (ADR-010, §3).</summary>
internal sealed class RegistrarSalaValidator : AbstractValidator<RegistrarSalaCommand>
{
    public RegistrarSalaValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");
    }
}
