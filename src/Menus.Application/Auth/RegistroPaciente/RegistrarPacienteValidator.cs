using FluentValidation;
using Menus.Application.Auth;

namespace Menus.Application.Auth.RegistroPaciente;

/// <summary>Validador para <see cref="RegistrarPacienteCommand"/>.</summary>
public sealed class RegistrarPacienteValidator : AbstractValidator<RegistrarPacienteCommand>
{
    public RegistrarPacienteValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .MaximumLength(254).WithMessage("El email no puede exceder 254 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres.");

        RuleFor(x => x.Edad)
            .GreaterThanOrEqualTo(0).WithMessage("La edad no puede ser negativa.")
            .LessThanOrEqualTo(150).WithMessage("La edad no puede exceder 150 años.");

        RuleFor(x => x.SalaId)
            .NotEmpty().WithMessage("La sala es obligatoria.");
    }
}