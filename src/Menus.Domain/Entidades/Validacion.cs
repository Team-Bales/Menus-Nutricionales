using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Aprobación de un menú por un nutricionista revisor (relación <c>Valida</c>, con sus atributos).
/// Solo la crea <see cref="Menu.Aprobar"/>, que comprueba las reglas del dictamen.
/// </summary>
/// <remarks>
/// <para>
/// <b>Un menú tiene a lo sumo una validación (R7-32), y por eso su identidad es el menú.</b> El primer dictamen
/// cierra el menú y el rechazo lo elimina (decisión del equipo, 16/10): un menú no llega a tener dos dictámenes.
/// El profesor señaló que el modelo admitía varias validaciones de revisores distintos sin una fecha comparable
/// entre ellas, y dos definiciones de «menú final» (feedback del 19/10). Con una sola validación por menú, final
/// es el menú que la tiene, sin ambigüedad. Si la clave siguiera siendo el par (revisor, menú), no sería mínima:
/// el menú ya basta para identificar la validación.
/// </para>
/// <para>
/// <b>No se modifica ni se borra (R7-33).</b> No tiene métodos para cambiarla, y la base rechaza el UPDATE y el
/// DELETE: si se pudiera borrar, un menú ya asignado o servido volvería a estar pendiente.
/// </para>
/// <para>
/// <b>Por qué no tiene un atributo «Aprobado».</b> El rechazo no se registra (decisión del equipo, 16/10), así que
/// toda validación es una aprobación: que la fila exista ya dice que el menú se aprobó. Un atributo booleano
/// valdría siempre lo mismo; sería la dependencia ∅ → aprobado, que viola la 3FN porque ∅ no es superclave, y la
/// base no podría impedir que alguien lo cambie y deje al menú ni pendiente ni final. Por eso se quitó (revisión
/// del 19/10); el MERX y R7-17 dejan de incluirlo en la fase de documentación.
/// </para>
/// </remarks>
public sealed class Validacion : Entity<Guid>
{
    /// <param name="menuId">Identificador del menú validado, que identifica la validación.</param>
    /// <param name="nutricionistaId">Identificador del nutricionista que valida.</param>
    /// <param name="fechaValidacion">Fecha y hora en que se emite el dictamen.</param>
    /// <param name="observaciones">Señalamientos del revisor, si existen.</param>
    internal Validacion(
        Guid menuId,
        Guid nutricionistaId,
        DateTime fechaValidacion,
        string? observaciones = null)
        : base(menuId)
    {
        if (fechaValidacion.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("El instante de validación debe estar en UTC (DateTimeKind.Utc).", nameof(fechaValidacion));
        }

        NutricionistaId = nutricionistaId;
        FechaValidacion = fechaValidacion;
        Observaciones = observaciones;
    }

    /// <summary>FK → Menú validado, que es también la clave (R7-32).</summary>
    public Guid MenuId => Id;

    /// <summary>FK → Nutricionista revisor.</summary>
    public Guid NutricionistaId { get; }

    /// <summary>Fecha y hora de la aprobación.</summary>
    public DateTime FechaValidacion { get; }

    /// <summary>Observaciones del revisor; puede ser <see langword="null"/>.</summary>
    public string? Observaciones { get; }
}
