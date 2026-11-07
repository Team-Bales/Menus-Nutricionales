using FluentValidation;

namespace Menus.Application.Catalogo.Nomencladores.GruposNutricionales;

/// <summary>Forma de la entrada de <see cref="RegistrarGrupoNutricionalCommand"/> (ADR-010, §3).</summary>
internal sealed class RegistrarGrupoNutricionalValidator : AbstractValidator<RegistrarGrupoNutricionalCommand>
{
    /// <summary>Largo máximo del nombre: el de la columna <c>grupos_nutricionales.nombre</c>.</summary>
    internal const int LargoMaximoDelNombre = 100;

    public RegistrarGrupoNutricionalValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(LargoMaximoDelNombre).WithMessage($"El nombre no puede tener más de {LargoMaximoDelNombre} caracteres.");
    }
}
