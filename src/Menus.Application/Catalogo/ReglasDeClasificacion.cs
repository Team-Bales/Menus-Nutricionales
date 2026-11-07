using FluentValidation;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo;

/// <summary>
/// Reglas de forma de la clasificación de un ítem (alimento o plato), compartidas por los validadores que la
/// reciben o filtran por ella.
/// </summary>
internal static class ReglasDeClasificacion
{
    /// <summary>Largo máximo del nombre: el de la columna <c>items_alimentarios.nombre</c>.</summary>
    internal const int LargoMaximoDelNombre = 150;

    /// <summary>El nombre es obligatorio (RD-04) y cabe en la columna.</summary>
    public static IRuleBuilderOptions<T, string> Nombre<T>(this IRuleBuilder<T, string> regla)
        => regla
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(LargoMaximoDelNombre).WithMessage($"El nombre no puede tener más de {LargoMaximoDelNombre} caracteres.");

    /// <summary>El grupo nutricional es obligatorio. Que exista lo garantiza la base (RR-01).</summary>
    public static IRuleBuilderOptions<T, Guid> GrupoNutricional<T>(this IRuleBuilder<T, Guid> regla)
        => regla.NotEmpty().WithMessage("El grupo nutricional es obligatorio.");

    /// <summary>El tipo de preparación tiene que ser del catálogo cerrado (RD-01).</summary>
    public static IRuleBuilderOptions<T, Guid> TipoDePreparacion<T>(this IRuleBuilder<T, Guid> regla)
        => regla.Must(TipoPreparacion.Existe).WithMessage("'{PropertyValue}' no es un tipo de preparación del catálogo.");

    /// <summary>El nivel calórico tiene que ser del catálogo cerrado (RD-02).</summary>
    public static IRuleBuilderOptions<T, Guid> NivelCalorico<T>(this IRuleBuilder<T, Guid> regla)
        => regla.Must(Domain.Nomenclators.NivelCalorico.Existe).WithMessage("'{PropertyValue}' no es un nivel calórico del catálogo.");

    /// <summary>La cantidad de un ingrediente es positiva (RD-05).</summary>
    public static IRuleBuilderOptions<T, decimal> Cantidad<T>(this IRuleBuilder<T, decimal> regla)
        => regla.GreaterThan(0m).WithMessage("La cantidad debe ser mayor que cero.");

    /// <summary>Filtro opcional por tipo de preparación: si viene, tiene que ser del catálogo (RD-01).</summary>
    public static IRuleBuilderOptions<T, Guid?> TipoDePreparacion<T>(this IRuleBuilder<T, Guid?> regla)
        => regla.Must(id => id is null || TipoPreparacion.Existe(id.Value))
            .WithMessage("'{PropertyValue}' no es un tipo de preparación del catálogo.");

    /// <summary>Filtro opcional por nivel calórico: si viene, tiene que ser del catálogo (RD-02).</summary>
    public static IRuleBuilderOptions<T, Guid?> NivelCalorico<T>(this IRuleBuilder<T, Guid?> regla)
        => regla.Must(id => id is null || Domain.Nomenclators.NivelCalorico.Existe(id.Value))
            .WithMessage("'{PropertyValue}' no es un nivel calórico del catálogo.");
}
