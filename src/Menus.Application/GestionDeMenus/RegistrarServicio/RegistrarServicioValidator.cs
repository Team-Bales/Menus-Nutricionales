using FluentValidation;

namespace Menus.Application.GestionDeMenus.RegistrarServicio;

/// <summary>Forma de la entrada de <see cref="RegistrarServicioCommand"/> (ADR-010, §3).</summary>
/// <remarks>
/// Que el día no sea futuro ni anterior a la aprobación (RS-14), y que cada ítem esté en el menú (RX-01), dependen del
/// menú: los comprueba el dominio.
/// </remarks>
internal sealed class RegistrarServicioValidator : AbstractValidator<RegistrarServicioCommand>
{
    public RegistrarServicioValidator()
    {
        RuleFor(c => c.PacienteId).NotEmpty().WithMessage("El paciente es obligatorio.");
        RuleFor(c => c.Fecha).NotEmpty().WithMessage("El día del servicio es obligatorio.");
        RuleFor(c => c.Consumos).NotNull().WithMessage("La lista de consumos es obligatoria; puede estar vacía.");

        RuleForEach(c => c.Consumos)
            .Must(c => c.ItemId != Guid.Empty)
            .WithMessage("Cada consumo indica el ítem.");

        RuleFor(c => c.Consumos)
            .Must(c => c.Select(x => x.ItemId).Distinct().Count() == c.Count)
            .When(c => c.Consumos is not null)
            .WithMessage("Un ítem aparece más de una vez: en cada servicio se registra una sola vez (RC-14).");
    }
}
