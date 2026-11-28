using System.ComponentModel.DataAnnotations;

namespace Menus.Client.GestionDeMenus;

/// <summary>Un nutricionista que creó o dictaminó un menú, con su nombre.</summary>
/// <param name="Id">Identificador del nutricionista.</param>
/// <param name="Nombre">Nombre del nutricionista.</param>
public sealed record NutricionistaDelMenu(Guid Id, string Nombre);

/// <summary>El dictamen de un menú.</summary>
/// <param name="Revisor">Jefe de nutrición que lo dictaminó.</param>
/// <param name="Fecha">Instante del dictamen, en UTC.</param>
/// <param name="Aprobado"><see langword="true"/> si lo aprobó; <see langword="false"/> si lo rechazó.</param>
/// <param name="Observaciones">Señalamientos del revisor, o nulo si no los hay.</param>
public sealed record DictamenDelMenu(NutricionistaDelMenu Revisor, DateTime Fecha, bool Aprobado, string? Observaciones);

/// <summary>Un menú sin su parametrización ni sus ítems: la forma que devuelve el listado (#80, F1).</summary>
/// <param name="Id">Identificador del menú.</param>
/// <param name="DietaId">Dieta a la que pertenece.</param>
/// <param name="FechaCreacion">Instante de creación, en UTC.</param>
/// <param name="Creador">Nutricionista que lo creó.</param>
/// <param name="EsAutomatico">Si se generó automáticamente.</param>
/// <param name="CantidadTotalAlimentos">Cantidad total de alimentos, parámetro de generación.</param>
/// <param name="Estado"><c>Pendiente</c>, <c>Final</c> o <c>Rechazado</c>, como texto.</param>
/// <param name="Dictamen">El dictamen, o nulo si está pendiente.</param>
/// <param name="ReemplazaA">El menú rechazado al que reemplaza, o nulo.</param>
public sealed record MenuResumen(
    Guid Id,
    Guid DietaId,
    DateTime FechaCreacion,
    NutricionistaDelMenu Creador,
    bool EsAutomatico,
    int CantidadTotalAlimentos,
    string Estado,
    DictamenDelMenu? Dictamen,
    Guid? ReemplazaA);

/// <summary>La proporción de alimentos de un tipo de preparación en un menú.</summary>
/// <param name="TipoPreparacionId">Identificador del tipo de preparación.</param>
/// <param name="Nombre">Nombre del tipo de preparación.</param>
/// <param name="Proporcion">Proporción, en (0, 1].</param>
public sealed record ProporcionDelMenu(Guid TipoPreparacionId, string Nombre, decimal Proporcion);

/// <summary>Un grupo nutricional o un ítem del menú, con su nombre.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nombre">Nombre.</param>
public sealed record ElementoDelMenu(Guid Id, string Nombre);

/// <summary>Un menú con su parametrización y sus ítems: la forma que devuelve el detalle (#80).</summary>
/// <param name="Id">Identificador del menú.</param>
/// <param name="DietaId">Dieta a la que pertenece.</param>
/// <param name="FechaCreacion">Instante de creación, en UTC.</param>
/// <param name="Creador">Nutricionista que lo creó.</param>
/// <param name="EsAutomatico">Si se generó automáticamente.</param>
/// <param name="CantidadTotalAlimentos">Cantidad total de alimentos, parámetro de generación.</param>
/// <param name="Estado"><c>Pendiente</c>, <c>Final</c> o <c>Rechazado</c>, como texto.</param>
/// <param name="Dictamen">El dictamen, o nulo si está pendiente.</param>
/// <param name="ReemplazaA">El menú rechazado al que reemplaza, o nulo.</param>
/// <param name="Distribucion">Proporción por tipo de preparación.</param>
/// <param name="GruposNutricionales">Grupos que cubre.</param>
/// <param name="Items">Alimentos y platos que incluye.</param>
public sealed record MenuDetalle(
    Guid Id,
    Guid DietaId,
    DateTime FechaCreacion,
    NutricionistaDelMenu Creador,
    bool EsAutomatico,
    int CantidadTotalAlimentos,
    string Estado,
    DictamenDelMenu? Dictamen,
    Guid? ReemplazaA,
    IReadOnlyList<ProporcionDelMenu> Distribucion,
    IReadOnlyList<ElementoDelMenu> GruposNutricionales,
    IReadOnlyList<ElementoDelMenu> Items);

/// <summary>Identificador del menú recién creado, tal como lo devuelve la API.</summary>
/// <param name="Id">Identificador del menú.</param>
public sealed record MenuCreado(Guid Id);

/// <summary>Una proporción de generación: fracción en (0, 1] de un tipo de preparación.</summary>
/// <param name="TipoPreparacionId">Identificador del tipo de preparación.</param>
/// <param name="Proporcion">Proporción como fracción.</param>
public sealed record ProporcionDeGeneracion(Guid TipoPreparacionId, decimal Proporcion);

/// <summary>Los parámetros con los que se pide generar un menú, ya en el formato de la API.</summary>
/// <param name="DietaId">Dieta para la que se genera.</param>
/// <param name="CantidadTotalAlimentos">Cantidad total de ítems del menú.</param>
/// <param name="Distribucion">Proporción por tipo de preparación; debe sumar 1.</param>
/// <param name="GruposNutricionalesIds">Grupos nutricionales que debe cubrir.</param>
public sealed record SolicitudDeGeneracion(
    Guid DietaId,
    int CantidadTotalAlimentos,
    IReadOnlyList<ProporcionDeGeneracion> Distribucion,
    IReadOnlyList<Guid> GruposNutricionalesIds);

/// <summary>Una fila del formulario de generación: el porcentaje de un tipo de preparación.</summary>
public sealed class ProporcionFormulario
{
    /// <summary>Tipo de preparación elegido.</summary>
    public Guid TipoPreparacionId { get; set; }

    /// <summary>
    /// Porcentaje de ítems de ese tipo; 0 significa que el tipo no entra en el menú. Los porcentajes de las filas
    /// incluidas deben sumar 100.
    /// </summary>
    public int Porcentaje { get; set; }
}

/// <summary>
/// Formulario de generación automática. Valida en el cliente lo mismo que exige la API (#80): dieta elegida,
/// cantidad positiva, proporciones entre 0 y 100 de las que al menos una es positiva, que sumen 100 sin repetir
/// tipos, y al menos un grupo nutricional. Un tipo con porcentaje 0 se omite, como admite el dominio (RD-11).
/// </summary>
public sealed class GenerarMenuFormulario : IValidatableObject
{
    /// <summary>Dieta elegida; <see cref="Guid.Empty"/> mientras no se elige.</summary>
    public Guid DietaId { get; set; }

    /// <summary>Cantidad total de alimentos del menú.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad total debe ser mayor que cero.")]
    public int CantidadTotalAlimentos { get; set; } = 10;

    /// <summary>Proporciones por tipo de preparación, en porcentaje.</summary>
    public List<ProporcionFormulario> Distribucion { get; set; } = [];

    /// <summary>Grupos nutricionales que debe cubrir el menú.</summary>
    public List<Guid> GruposNutricionalesIds { get; set; } = [];

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DietaId == Guid.Empty)
        {
            yield return new("Selecciona una dieta.", [nameof(DietaId)]);
        }

        if (Distribucion.Count == 0)
        {
            yield return new("Indica la proporción de al menos un tipo de preparación.", [nameof(Distribucion)]);
        }
        else
        {
            if (Distribucion.Any(d => d.Porcentaje < 0 || d.Porcentaje > 100))
            {
                yield return new("Cada porcentaje debe estar entre 0 y 100.", [nameof(Distribucion)]);
            }

            if (Distribucion.All(d => d.Porcentaje == 0))
            {
                yield return new("Indica el porcentaje de al menos un tipo de preparación.", [nameof(Distribucion)]);
            }

            if (Distribucion.Select(d => d.TipoPreparacionId).Distinct().Count() != Distribucion.Count)
            {
                yield return new("No se puede repetir un tipo de preparación.", [nameof(Distribucion)]);
            }

            if (Distribucion.Sum(d => d.Porcentaje) != 100)
            {
                yield return new("Los porcentajes deben sumar 100.", [nameof(Distribucion)]);
            }
        }

        if (GruposNutricionalesIds.Count == 0)
        {
            yield return new("Selecciona al menos un grupo nutricional.", [nameof(GruposNutricionalesIds)]);
        }
    }
}
