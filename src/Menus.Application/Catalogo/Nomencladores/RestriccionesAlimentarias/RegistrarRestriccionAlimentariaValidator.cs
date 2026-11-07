using FluentValidation;

namespace Menus.Application.Catalogo.Nomencladores.RestriccionesAlimentarias;

/// <summary>Forma de la entrada de <see cref="RegistrarRestriccionAlimentariaCommand"/> (ADR-010, §3).</summary>
internal sealed class RegistrarRestriccionAlimentariaValidator : AbstractValidator<RegistrarRestriccionAlimentariaCommand>
{
    /// <summary>Largo máximo del nombre: el de la columna <c>restricciones_alimentarias.nombre</c>.</summary>
    internal const int LargoMaximoDelNombre = 100;

    public RegistrarRestriccionAlimentariaValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(LargoMaximoDelNombre).WithMessage($"El nombre no puede tener más de {LargoMaximoDelNombre} caracteres.");
    }
}
