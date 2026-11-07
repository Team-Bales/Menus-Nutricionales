using FluentValidation;

namespace Menus.Application.Catalogo.Nomencladores.Especialidades;

/// <summary>Forma de la entrada de <see cref="RegistrarEspecialidadCommand"/> (ADR-010, §3).</summary>
internal sealed class RegistrarEspecialidadValidator : AbstractValidator<RegistrarEspecialidadCommand>
{
    /// <summary>Largo máximo del nombre: el de la columna <c>especialidades.nombre</c>.</summary>
    internal const int LargoMaximoDelNombre = 100;

    public RegistrarEspecialidadValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(LargoMaximoDelNombre).WithMessage($"El nombre no puede tener más de {LargoMaximoDelNombre} caracteres.");
    }
}
