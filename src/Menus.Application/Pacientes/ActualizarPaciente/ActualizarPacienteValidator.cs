using FluentValidation;

namespace Menus.Application.Pacientes.ActualizarPaciente;

/// <summary>Forma de la entrada de <see cref="ActualizarPacienteCommand"/> (ADR-010, §3).</summary>
internal sealed class ActualizarPacienteValidator : AbstractValidator<ActualizarPacienteCommand>
{
    public ActualizarPacienteValidator()
    {
        RuleFor(c => c.Nombre).Nombre();
        RuleFor(c => c.Edad).Edad();
        RuleFor(c => c.SalaId).Sala();
    }
}
