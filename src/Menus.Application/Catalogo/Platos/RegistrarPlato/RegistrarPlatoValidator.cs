using FluentValidation;

namespace Menus.Application.Catalogo.Platos.RegistrarPlato;

/// <summary>Forma de la entrada de <see cref="RegistrarPlatoCommand"/> (ADR-010, §3).</summary>
internal sealed class RegistrarPlatoValidator : AbstractValidator<RegistrarPlatoCommand>
{
    public RegistrarPlatoValidator()
    {
        RuleFor(c => c.Nombre).Nombre();
        RuleFor(c => c.GrupoNutricionalId).GrupoNutricional();
        RuleFor(c => c.TipoPreparacionId).TipoDePreparacion();
        RuleFor(c => c.NivelCaloricoId).NivelCalorico();
        RuleFor(c => c.Ingredientes)
            .NotEmpty().WithMessage("Un plato se compone de al menos un alimento (RK-03).");
        // Regla aparte: un When en la misma cadena también apagaría el NotEmpty cuando la lista es nula.
        RuleFor(c => c.Ingredientes)
            .Must(i => i.Select(x => x.AlimentoId).Distinct().Count() == i.Count)
            .When(c => c.Ingredientes is not null)
            .WithMessage("Un alimento no se repite en la composición de un plato.");
        RuleForEach(c => c.Ingredientes).SetValidator(new IngredienteDelPlatoValidator());
    }
}

/// <summary>Forma de un ingrediente: el alimento y una cantidad positiva (RD-05).</summary>
internal sealed class IngredienteDelPlatoValidator : AbstractValidator<IngredienteDelPlato>
{
    public IngredienteDelPlatoValidator()
    {
        RuleFor(i => i.AlimentoId).NotEmpty().WithMessage("El alimento es obligatorio.");
        RuleFor(i => i.Cantidad).Cantidad();
    }
}
