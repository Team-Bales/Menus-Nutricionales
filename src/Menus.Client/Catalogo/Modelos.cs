using System.ComponentModel.DataAnnotations;

namespace Menus.Client.Catalogo;

/// <summary>Elemento que se muestra en un selector de nomencladores.</summary>
public sealed record Nomenclador(Guid Id, string Nombre);

/// <summary>Resultado paginado que devuelve la API.</summary>
public sealed record Pagina<T>(IReadOnlyList<T> Data, int Total, int Page, int Size);

/// <summary>Dieta mostrada en un listado, con los conteos que requiere el catálogo.</summary>
public sealed record Dieta(Guid Id, string Nombre, Guid ProgramaAtencionId, int CantidadDeGrupos, int CantidadDeRestricciones);

/// <summary>Detalle de una dieta con sus grupos nutricionales y restricciones alimentarias.</summary>
public sealed record DietaDetalle(Guid Id, string Nombre, Guid ProgramaAtencionId, IReadOnlyList<GrupoCubierto> Grupos, IReadOnlyList<Guid> Restricciones);

/// <summary>Grupo nutricional cubierto por una dieta y el tipo de cobertura aplicado.</summary>
public sealed record GrupoCubierto(Guid GrupoNutricionalId, TipoCobertura Tipo);

/// <summary>Tipo de cobertura de un grupo nutricional en una dieta.</summary>
public enum TipoCobertura
{
    /// <summary>El grupo debe estar presente en el menú.</summary>
    Requerido,

    /// <summary>El grupo no debe estar presente en el menú.</summary>
    Restringido,
}

/// <summary>Catálogos institucionales que pueden administrarse desde la interfaz.</summary>
public enum TipoDeNomenclador
{
    /// <summary>Grupos nutricionales.</summary>
    GruposNutricionales,

    /// <summary>Restricciones alimentarias.</summary>
    RestriccionesAlimentarias,

    /// <summary>Programas de atención.</summary>
    ProgramasAtencion,

    /// <summary>Especialidades de nutricionistas.</summary>
    Especialidades,
}

/// <summary>Datos necesarios para agregar un grupo a la cobertura de una dieta.</summary>
public sealed class GrupoCubiertoFormulario
{
    /// <summary>Identificador del grupo nutricional.</summary>
    public Guid GrupoNutricionalId { get; set; }

    /// <summary>Tipo de cobertura que tendrá el grupo.</summary>
    public TipoCobertura Tipo { get; set; } = TipoCobertura.Requerido;
}

/// <summary>Formulario mínimo para crear un elemento de nomenclador.</summary>
public sealed class NomencladorFormulario
{
    /// <summary>Nombre visible del elemento.</summary>
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}

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
