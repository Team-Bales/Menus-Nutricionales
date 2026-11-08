using FluentValidation;

namespace Menus.Application.Pacientes;

/// <summary>Reglas de forma de los datos de un paciente, compartidas por los validadores que los reciben.</summary>
internal static class ReglasDePaciente
{
    /// <summary>Largo máximo del nombre: el de la columna <c>pacientes.nombre</c>.</summary>
    internal const int LargoMaximoDelNombre = 150;

    /// <summary>El nombre es obligatorio (RD-07) y cabe en la columna.</summary>
    public static IRuleBuilderOptions<T, string> Nombre<T>(this IRuleBuilder<T, string> regla)
        => regla
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(LargoMaximoDelNombre).WithMessage($"El nombre no puede tener más de {LargoMaximoDelNombre} caracteres.");

    /// <summary>
    /// La edad es mayor o igual que cero (RD-06). Cero es válido: la edad se registra en años enteros (RD-09), y un
    /// paciente de menos de un año tiene cero.
    /// </summary>
    public static IRuleBuilderOptions<T, int> Edad<T>(this IRuleBuilder<T, int> regla)
        => regla.GreaterThanOrEqualTo(0).WithMessage("La edad no puede ser negativa.");

    /// <summary>La sala es obligatoria. Que exista lo comprueba el caso de uso.</summary>
    public static IRuleBuilderOptions<T, Guid> Sala<T>(this IRuleBuilder<T, Guid> regla)
        => regla.NotEmpty().WithMessage("La sala es obligatoria.");

    /// <summary>La dieta es obligatoria. Que exista lo comprueba el caso de uso.</summary>
    public static IRuleBuilderOptions<T, Guid> Dieta<T>(this IRuleBuilder<T, Guid> regla)
        => regla.NotEmpty().WithMessage("La dieta es obligatoria.");
}
