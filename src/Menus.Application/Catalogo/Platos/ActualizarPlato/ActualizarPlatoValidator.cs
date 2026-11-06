using FluentValidation;

namespace Menus.Application.Catalogo.Platos.ActualizarPlato;

/// <summary>Forma de la entrada de <see cref="ActualizarPlatoCommand"/> (ADR-010, §3).</summary>
internal sealed class ActualizarPlatoValidator : AbstractValidator<ActualizarPlatoCommand>
{
    public ActualizarPlatoValidator()
    {
        RuleFor(c => c.GrupoNutricionalId).GrupoNutricional();
        RuleFor(c => c.TipoPreparacionId).TipoDePreparacion();
        RuleFor(c => c.NivelCaloricoId).NivelCalorico();
    }
}
