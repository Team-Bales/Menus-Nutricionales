using FluentValidation;
using Menus.Application.Revalorizaciones.SolicitarRevalorizacion;

namespace Menus.Application.Revalorizaciones.SolicitarRevalorizacion;

/// <summary>Validador de <see cref="SolicitarRevalorizacionCommand"/>.</summary>
internal sealed class SolicitarRevalorizacionValidator : AbstractValidator<SolicitarRevalorizacionCommand>
{
    public SolicitarRevalorizacionValidator()
    {
        RuleFor(c => c.ValoracionRevisadaId)
            .NotEmpty().WithMessage("El identificador de la valoración revisada es obligatorio.");

        RuleFor(c => c.NutricionistaId)
            .NotEmpty().WithMessage("El identificador del nutricionista es obligatorio.");
    }
}
