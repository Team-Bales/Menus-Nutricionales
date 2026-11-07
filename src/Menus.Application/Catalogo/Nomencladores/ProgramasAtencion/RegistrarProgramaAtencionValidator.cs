using FluentValidation;

namespace Menus.Application.Catalogo.Nomencladores.ProgramasAtencion;

/// <summary>Forma de la entrada de <see cref="RegistrarProgramaAtencionCommand"/> (ADR-010, §3).</summary>
internal sealed class RegistrarProgramaAtencionValidator : AbstractValidator<RegistrarProgramaAtencionCommand>
{
    /// <summary>Largo máximo del nombre: el de la columna <c>programas_atencion.nombre</c>.</summary>
    internal const int LargoMaximoDelNombre = 100;

    public RegistrarProgramaAtencionValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(LargoMaximoDelNombre).WithMessage($"El nombre no puede tener más de {LargoMaximoDelNombre} caracteres.");
    }
}
