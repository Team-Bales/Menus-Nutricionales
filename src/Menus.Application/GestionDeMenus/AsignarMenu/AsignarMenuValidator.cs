using FluentValidation;

namespace Menus.Application.GestionDeMenus.AsignarMenu;

/// <summary>Forma de la entrada de <see cref="AsignarMenuCommand"/> (ADR-010, §3).</summary>
internal sealed class AsignarMenuValidator : AbstractValidator<AsignarMenuCommand>
{
    public AsignarMenuValidator()
    {
        RuleFor(c => c.PacienteId).NotEmpty().WithMessage("El paciente es obligatorio.");
    }
}
