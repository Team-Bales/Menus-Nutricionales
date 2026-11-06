using FluentValidation;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Alimentos;

/// <summary>
/// Reglas de forma de la clasificación de un ítem, compartidas por los validadores que la reciben.
/// </summary>
internal static class ReglasDeClasificacion
{
    /// <summary>El grupo nutricional es obligatorio. Que exista lo garantiza la base (RR-01).</summary>
    public static IRuleBuilderOptions<T, Guid> GrupoNutricional<T>(this IRuleBuilder<T, Guid> regla)
        => regla.NotEmpty().WithMessage("El grupo nutricional es obligatorio.");

    /// <summary>El tipo de preparación tiene que ser del catálogo cerrado (RD-01).</summary>
    public static IRuleBuilderOptions<T, Guid> TipoDePreparacion<T>(this IRuleBuilder<T, Guid> regla)
        => regla.Must(TipoPreparacion.Existe).WithMessage("'{PropertyValue}' no es un tipo de preparación del catálogo.");

    /// <summary>El nivel calórico tiene que ser del catálogo cerrado (RD-02).</summary>
    public static IRuleBuilderOptions<T, Guid> NivelCalorico<T>(this IRuleBuilder<T, Guid> regla)
        => regla.Must(Domain.Nomenclators.NivelCalorico.Existe).WithMessage("'{PropertyValue}' no es un nivel calórico del catálogo.");
}
