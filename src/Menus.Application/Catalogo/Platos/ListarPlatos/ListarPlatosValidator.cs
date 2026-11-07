using FluentValidation;
using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Platos.ListarPlatos;

/// <summary>Forma de la entrada de <see cref="ListarPlatosQuery"/> (ADR-010, §3).</summary>
internal sealed class ListarPlatosValidator : AbstractValidator<ListarPlatosQuery>
{
    public ListarPlatosValidator()
    {
        RuleFor(q => q.Page).Pagina();
        RuleFor(q => q.Size).TamanoDePagina();
        RuleFor(q => q.TipoPreparacionId).TipoDePreparacion();
        RuleFor(q => q.NivelCaloricoId).NivelCalorico();
    }
}
