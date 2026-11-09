using FluentValidation;

namespace Menus.Application.Pacientes.InscribirDieta;

/// <summary>Forma de la entrada de <see cref="InscribirDietaCommand"/> (ADR-010, §3).</summary>
internal sealed class InscribirDietaValidator : AbstractValidator<InscribirDietaCommand>
{
    public InscribirDietaValidator()
    {
        RuleFor(c => c.DietaId).Dieta();
    }
}
