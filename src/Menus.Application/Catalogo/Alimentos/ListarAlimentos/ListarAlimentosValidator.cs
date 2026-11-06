using FluentValidation;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Alimentos.ListarAlimentos;

/// <summary>Forma de la entrada de <see cref="ListarAlimentosQuery"/> (ADR-010, §3).</summary>
internal sealed class ListarAlimentosValidator : AbstractValidator<ListarAlimentosQuery>
{
    /// <summary>Tope de alimentos por página, para que un cliente no pida el banco entero de una vez.</summary>
    internal const int TamanoMaximo = 100;

    public ListarAlimentosValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1).WithMessage("La página empieza en 1.");
        RuleFor(q => q.Size)
            .InclusiveBetween(1, TamanoMaximo).WithMessage($"El tamaño de página va de 1 a {TamanoMaximo}.");
        RuleFor(q => q.TipoPreparacionId)
            .Must(id => TipoPreparacion.Existe(id!.Value)).When(q => q.TipoPreparacionId is not null)
            .WithMessage("'{PropertyValue}' no es un tipo de preparación del catálogo.");
        RuleFor(q => q.NivelCaloricoId)
            .Must(id => NivelCalorico.Existe(id!.Value)).When(q => q.NivelCaloricoId is not null)
            .WithMessage("'{PropertyValue}' no es un nivel calórico del catálogo.");
    }
}
