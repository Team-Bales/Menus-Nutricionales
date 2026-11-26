using FluentValidation;

namespace Menus.Application.Catalogo.Fichas.GuardarFicha;

/// <summary>
/// Forma de la entrada de <see cref="GuardarFichaCommand"/> (ADR-010, §3): RM-04, y nombres de campo que MongoDB pueda
/// guardar en los bloques libres (RM-05). Lo que depende del ítem (RM-03, RM-06 y RS-02) lo comprueba el manejador.
/// </summary>
internal sealed class GuardarFichaValidator : AbstractValidator<GuardarFichaCommand>
{
    private const string MensajeDeNombres =
        "Los nombres de campo de {0} no pueden estar vacíos, empezar con «$» ni contener «.».";

    public GuardarFichaValidator()
    {
        RuleFor(c => c.ItemId).NotEmpty().WithMessage("El ítem es obligatorio.");

        RuleFor(c => c.PorcionReferencia!.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad de la porción de referencia tiene que ser positiva (RM-04).")
            .Must(double.IsFinite).WithMessage("La cantidad de la porción de referencia tiene que ser un número.")
            .When(c => c.PorcionReferencia is not null)
            .OverridePropertyName("porcionReferencia.cantidad");
        RuleFor(c => c.PorcionReferencia!.Unidad)
            .NotEmpty().WithMessage("La unidad de la porción de referencia es obligatoria.")
            .MaximumLength(50).WithMessage("La unidad de la porción de referencia no puede exceder 50 caracteres.")
            .When(c => c.PorcionReferencia is not null)
            .OverridePropertyName("porcionReferencia.unidad");

        RuleForEach(c => c.Alergenos)
            .NotEmpty().WithMessage("Un alérgeno no puede estar vacío.")
            .MaximumLength(100).WithMessage("Un alérgeno no puede exceder 100 caracteres.");
        // Regla aparte, como en RegistrarPlatoValidator: un When en la misma cadena apagaría también las de cada elemento.
        RuleFor(c => c.Alergenos)
            .Must(a => a!.Select(ReglasDeLaFicha.NormalizarAlergeno).Distinct().Count() == a!.Count)
            .When(c => c.Alergenos is not null && c.Alergenos.All(a => !string.IsNullOrWhiteSpace(a)))
            .WithMessage("Un alérgeno no se repite en la ficha, sin distinguir mayúsculas ni acentos (RM-04).");

        RuleFor(c => c.Macronutrientes)
            .Must(ReglasDeLaFicha.NombresDeCampoValidos).WithMessage(string.Format(MensajeDeNombres, "macronutrientes"));
        RuleFor(c => c.Micronutrientes)
            .Must(ReglasDeLaFicha.NombresDeCampoValidos).WithMessage(string.Format(MensajeDeNombres, "micronutrientes"));
        RuleFor(c => c.ModoPreparacion)
            .Must(ReglasDeLaFicha.NombresDeCampoValidos).WithMessage(string.Format(MensajeDeNombres, "modoPreparacion"));

        RuleForEach(c => c.Ingredientes).ChildRules(ingrediente =>
        {
            ingrediente.RuleFor(i => i.AlimentoId).NotEmpty().WithMessage("El alimento del ingrediente es obligatorio.");
            ingrediente.RuleFor(i => i.Aporte)
                .Must(ReglasDeLaFicha.NombresDeCampoValidos).WithMessage(string.Format(MensajeDeNombres, "aporte"));
        });
        RuleFor(c => c.Ingredientes)
            .Must(i => i!.Select(x => x.AlimentoId).Distinct().Count() == i!.Count)
            .When(c => c.Ingredientes is not null)
            .WithMessage("Un alimento no se repite en el desglose de ingredientes.");
    }
}
