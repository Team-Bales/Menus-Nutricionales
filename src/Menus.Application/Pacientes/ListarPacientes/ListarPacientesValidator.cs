using FluentValidation;
using Menus.Application.Comun;

namespace Menus.Application.Pacientes.ListarPacientes;

/// <summary>Forma de la entrada de <see cref="ListarPacientesQuery"/> (ADR-010, §3).</summary>
internal sealed class ListarPacientesValidator : AbstractValidator<ListarPacientesQuery>
{
    public ListarPacientesValidator()
    {
        RuleFor(q => q.Page).Pagina();
        RuleFor(q => q.Size).TamanoDePagina();
    }
}
