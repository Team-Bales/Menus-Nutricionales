using FluentValidation;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Dietas.RegistrarDieta;

/// <summary>Forma de la entrada de <see cref="RegistrarDietaCommand"/> (ADR-010, §3).</summary>
internal sealed class RegistrarDietaValidator : AbstractValidator<RegistrarDietaCommand>
{
    /// <summary>Largo máximo del nombre: el de la columna <c>dietas.nombre</c>.</summary>
    internal const int LargoMaximoDelNombre = 150;

    public RegistrarDietaValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(LargoMaximoDelNombre).WithMessage($"El nombre no puede tener más de {LargoMaximoDelNombre} caracteres.");

        RuleFor(c => c.ProgramaAtencionId)
            .NotEmpty().WithMessage("El programa de atención es obligatorio.");

        // Que el grupo y la restricción existan lo garantiza la base (RR-01); aquí solo se exige la forma mínima
        // que el dominio no comprueba por sí solo: al menos un grupo, y al menos uno requerido (RK-07).
        RuleFor(c => c.Grupos)
            .NotEmpty().WithMessage("La dieta tiene que cubrir al menos un grupo nutricional.");

        RuleFor(c => c.Grupos)
            .Must(g => g.Any(x => x.Tipo == TipoCobertura.Requerido))
            .When(c => c.Grupos.Count > 0)
            .WithMessage("La dieta tiene que exigir al menos un grupo nutricional requerido (RK-07).");
    }
}
