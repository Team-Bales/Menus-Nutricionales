using FluentValidation;
using Menus.Application.Catalogo.Dietas.RegistrarDieta;

namespace Menus.Application.Catalogo.Dietas.ActualizarDieta;

/// <summary>Forma de la entrada de <see cref="ActualizarDietaCommand"/> (ADR-010, §3).</summary>
internal sealed class ActualizarDietaValidator : AbstractValidator<ActualizarDietaCommand>
{
    public ActualizarDietaValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(RegistrarDietaValidator.LargoMaximoDelNombre)
                .WithMessage($"El nombre no puede tener más de {RegistrarDietaValidator.LargoMaximoDelNombre} caracteres.");

        RuleFor(c => c.ProgramaAtencionId)
            .NotEmpty().WithMessage("El programa de atención es obligatorio.");
    }
}
