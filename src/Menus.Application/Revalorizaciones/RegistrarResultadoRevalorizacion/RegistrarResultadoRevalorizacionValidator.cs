using FluentValidation;
using Menus.Application.Revalorizaciones.RegistrarResultadoRevalorizacion;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Revalorizaciones.RegistrarResultadoRevalorizacion;

/// <summary>Validador de <see cref="RegistrarResultadoRevalorizacionCommand"/>.</summary>
internal sealed class RegistrarResultadoRevalorizacionValidator : AbstractValidator<RegistrarResultadoRevalorizacionCommand>
{
    public RegistrarResultadoRevalorizacionValidator()
    {
        RuleFor(c => c.RevalorizacionId)
            .NotEmpty().WithMessage("El identificador de la revalorización es obligatorio.");

        RuleFor(c => c.ResultadoNutricionalId)
            .Must(ResultadoNutricional.Existe).WithMessage("El resultado nutricional no es una categoría válida del catálogo (RR-17).");
    }
}
