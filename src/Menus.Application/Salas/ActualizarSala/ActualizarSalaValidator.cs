using FluentValidation;

namespace Menus.Application.Salas.ActualizarSala;

/// <summary>Forma de la entrada de <see cref="ActualizarSalaCommand"/> (ADR-010, §3).</summary>
internal sealed class ActualizarSalaValidator : AbstractValidator<ActualizarSalaCommand>
{
    public ActualizarSalaValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");
    }
}
