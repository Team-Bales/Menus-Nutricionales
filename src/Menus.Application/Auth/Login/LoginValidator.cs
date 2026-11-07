using System.Text;
using FluentValidation;
using Menus.Application.Auth;

namespace Menus.Application.Auth.Login;

/// <summary>Validador para <see cref="LoginCommand"/>.</summary>
public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .MaximumLength(254).WithMessage("El email no puede exceder 254 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres.")
            .Must(ValidarLongitudBytesBcrypt).WithMessage("La contraseña no puede exceder 72 bytes (límite de BCrypt).");
    }

    private static bool ValidarLongitudBytesBcrypt(string password)
    {
        // BCrypt trunca a 72 bytes. Validamos que la codificación UTF-8 no exceda ese límite.
        return Encoding.UTF8.GetByteCount(password) <= 72;
    }
}
