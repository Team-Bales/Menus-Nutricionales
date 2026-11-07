using System.Text;
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
            .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres.")
            .Must(ValidarLongitudBytesBcrypt).WithMessage("La contraseña no puede exceder 72 bytes (límite de BCrypt).");

        RuleFor(x => x.Edad)
            .GreaterThan(0).WithMessage("La edad debe ser mayor que cero.")
            .LessThanOrEqualTo(150).WithMessage("La edad no puede exceder 150 años.");

        RuleFor(x => x.SalaId)
            .NotEmpty().WithMessage("La sala es obligatoria.");
    }

    private static bool ValidarLongitudBytesBcrypt(string password)
    {
        // BCrypt trunca a 72 bytes. Validamos que la codificación UTF-8 no exceda ese límite.
        return Encoding.UTF8.GetByteCount(password) <= 72;
    }
}
