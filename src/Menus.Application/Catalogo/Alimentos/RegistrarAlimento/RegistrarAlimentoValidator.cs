using FluentValidation;

namespace Menus.Application.Catalogo.Alimentos.RegistrarAlimento;

/// <summary>Forma de la entrada de <see cref="RegistrarAlimentoCommand"/> (ADR-010, §3).</summary>
internal sealed class RegistrarAlimentoValidator : AbstractValidator<RegistrarAlimentoCommand>
{
    public RegistrarAlimentoValidator()
    {
        RuleFor(c => c.Nombre).Nombre();
        RuleFor(c => c.GrupoNutricionalId).GrupoNutricional();
        RuleFor(c => c.TipoPreparacionId).TipoDePreparacion();
        RuleFor(c => c.NivelCaloricoId).NivelCalorico();
    }
}
