using FluentValidation;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Valoraciones.RegistrarValoracion;

/// <summary>Forma de la entrada de <see cref="RegistrarValoracionCommand"/> (ADR-010, §3).</summary>
/// <remarks>
/// Valida aquí que <see cref="RegistrarValoracionCommand.ResultadoNutricionalId"/> sea una categoría del catálogo:
/// el constructor de la entidad también lo comprueba, pero con un <see cref="ArgumentException"/> que no se traduce
/// a 422 (ADR-010, §4). Que el menú y el paciente existan, y que la asignación exista, lo comprueba el manejador.
/// </remarks>
internal sealed class RegistrarValoracionValidator : AbstractValidator<RegistrarValoracionCommand>
{
    public RegistrarValoracionValidator()
    {
        RuleFor(c => c.MenuId).NotEmpty().WithMessage("El menú es obligatorio.");
        RuleFor(c => c.PacienteId).NotEmpty().WithMessage("El paciente es obligatorio.");
        RuleFor(c => c.ResultadoNutricionalId)
            .Must(ResultadoNutricional.Existe)
            .WithMessage("El resultado nutricional no es una categoría válida.");
    }
}
