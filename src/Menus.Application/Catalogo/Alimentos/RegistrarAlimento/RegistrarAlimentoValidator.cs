using FluentValidation;

namespace Menus.Application.Catalogo.Alimentos.RegistrarAlimento;

/// <summary>Forma de la entrada de <see cref="RegistrarAlimentoCommand"/> (ADR-010, §3).</summary>
internal sealed class RegistrarAlimentoValidator : AbstractValidator<RegistrarAlimentoCommand>
{
    /// <summary>Largo máximo del nombre: el de la columna <c>items_alimentarios.nombre</c>.</summary>
    internal const int LargoMaximoDelNombre = 150;

    public RegistrarAlimentoValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(LargoMaximoDelNombre).WithMessage($"El nombre no puede tener más de {LargoMaximoDelNombre} caracteres.");
        RuleFor(c => c.GrupoNutricionalId).GrupoNutricional();
        RuleFor(c => c.TipoPreparacionId).TipoDePreparacion();
        RuleFor(c => c.NivelCaloricoId).NivelCalorico();
    }
}
