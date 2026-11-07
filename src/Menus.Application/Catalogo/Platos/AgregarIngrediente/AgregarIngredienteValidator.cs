using FluentValidation;

namespace Menus.Application.Catalogo.Platos.AgregarIngrediente;

/// <summary>Forma de la entrada de <see cref="AgregarIngredienteCommand"/> (ADR-010, §3).</summary>
internal sealed class AgregarIngredienteValidator : AbstractValidator<AgregarIngredienteCommand>
{
    public AgregarIngredienteValidator()
    {
        RuleFor(c => c.AlimentoId).NotEmpty().WithMessage("El alimento es obligatorio.");
        RuleFor(c => c.Cantidad).Cantidad();
    }
}
