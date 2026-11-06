using FluentValidation;

namespace Menus.Application.Catalogo.Alimentos.ActualizarAlimento;

/// <summary>Forma de la entrada de <see cref="ActualizarAlimentoCommand"/> (ADR-010, §3).</summary>
internal sealed class ActualizarAlimentoValidator : AbstractValidator<ActualizarAlimentoCommand>
{
    public ActualizarAlimentoValidator()
    {
        RuleFor(c => c.GrupoNutricionalId).GrupoNutricional();
        RuleFor(c => c.TipoPreparacionId).TipoDePreparacion();
        RuleFor(c => c.NivelCaloricoId).NivelCalorico();
    }
}
