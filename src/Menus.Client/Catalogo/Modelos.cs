using System.ComponentModel.DataAnnotations;

namespace Menus.Client.Catalogo;

/// <summary>Elemento que se muestra en un selector de nomencladores.</summary>
public sealed record Nomenclador(Guid Id, string Nombre);

/// <summary>Resultado paginado que devuelve la API.</summary>
public sealed record Pagina<T>(IReadOnlyList<T> Data, int Total, int Page, int Size);

/// <summary>Alimento del catálogo.</summary>
public sealed record Alimento(Guid Id, string Nombre, Guid GrupoNutricionalId, Guid TipoPreparacionId, Guid NivelCaloricoId, Guid NutricionistaId);

/// <summary>Plato del catálogo, sin sus ingredientes.</summary>
public sealed record Plato(Guid Id, string Nombre, Guid GrupoNutricionalId, Guid TipoPreparacionId, Guid NivelCaloricoId, Guid NutricionistaId, int CantidadDeIngredientes);

/// <summary>Ingrediente de un plato.</summary>
public sealed record Ingrediente(Guid AlimentoId, string Nombre, decimal Cantidad);

/// <summary>Plato con su composición.</summary>
public sealed record PlatoDetalle(Guid Id, string Nombre, Guid GrupoNutricionalId, Guid TipoPreparacionId, Guid NivelCaloricoId, Guid NutricionistaId, IReadOnlyList<Ingrediente> Ingredientes);

/// <summary>Datos de clasificación compartidos por alimentos y platos.</summary>
public sealed class ClasificacionFormulario : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(160, ErrorMessage = "El nombre no puede superar 160 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
    public Guid GrupoNutricionalId { get; set; }
    public Guid TipoPreparacionId { get; set; }
    public Guid NivelCaloricoId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (GrupoNutricionalId == Guid.Empty)
        {
            yield return new("Selecciona un grupo nutricional.", [nameof(GrupoNutricionalId)]);
        }

        if (TipoPreparacionId == Guid.Empty)
        {
            yield return new("Selecciona un tipo de preparación.", [nameof(TipoPreparacionId)]);
        }

        if (NivelCaloricoId == Guid.Empty)
        {
            yield return new("Selecciona un nivel calórico.", [nameof(NivelCaloricoId)]);
        }
    }
}

/// <summary>Ingrediente editable antes de enviar un plato.</summary>
public sealed class IngredienteFormulario
{
    public Guid AlimentoId { get; set; }
    public decimal Cantidad { get; set; } = 1m;
}
