using FluentValidation;

namespace Menus.Application.GeneracionDeMenus.GenerarMenuAutomaticamente;

/// <summary>
/// Forma de la entrada de <see cref="GenerarMenuAutomaticamenteCommand"/> (ADR-010, §3).
/// </summary>
/// <remarks>
/// Solo cubre lo estructural más simple (presencia). Las reglas finas de los parámetros de generación —suma de
/// proporciones igual a 1, sin tipos ni grupos repetidos, valores en rango— ya las valida cada value object del
/// #79 con <see cref="Menus.Domain.Excepciones.DatoInvalidoException"/>, que también se traduce a 422 (ADR-010,
/// §4): duplicarlas aquí no agrega nada.
/// </remarks>
internal sealed class GenerarMenuAutomaticamenteValidator : AbstractValidator<GenerarMenuAutomaticamenteCommand>
{
    public GenerarMenuAutomaticamenteValidator()
    {
        RuleFor(c => c.DietaId).NotEmpty().WithMessage("La dieta es obligatoria.");
        RuleFor(c => c.CantidadTotalAlimentos).GreaterThan(0)
            .WithMessage("La cantidad total de alimentos debe ser estrictamente positiva.");
        RuleFor(c => c.Distribucion).NotEmpty()
            .WithMessage("El menú debe tener al menos una proporción por tipo de preparación.");
        RuleFor(c => c.GruposNutricionalesIds).NotEmpty()
            .WithMessage("El menú debe cubrir al menos un grupo nutricional.");
    }
}
