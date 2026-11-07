using FluentValidation;
using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Alimentos.ListarAlimentos;

/// <summary>Forma de la entrada de <see cref="ListarAlimentosQuery"/> (ADR-010, §3).</summary>
internal sealed class ListarAlimentosValidator : AbstractValidator<ListarAlimentosQuery>
{
    public ListarAlimentosValidator()
    {
        RuleFor(q => q.Page).Pagina();
        RuleFor(q => q.Size).TamanoDePagina();
        RuleFor(q => q.TipoPreparacionId).TipoDePreparacion();
        RuleFor(q => q.NivelCaloricoId).NivelCalorico();
    }
}
