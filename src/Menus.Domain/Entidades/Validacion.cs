using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Dictamen de un nutricionista revisor sobre un menú: aprobación o rechazo (relación <c>Valida</c>, con sus
/// atributos). Solo la crean <see cref="Menu.Aprobar"/> y <see cref="Menu.Rechazar"/>, que comprueban las reglas
/// del dictamen.
/// </summary>
/// <remarks>
/// <para>
/// <b>Un menú tiene a lo sumo un dictamen (RC-12, RK-13), y por eso su identidad es el menú.</b> El primer dictamen
/// cierra el menú: si es una aprobación, el menú pasa a ser final; si es un rechazo, el menú queda rechazado y,
/// cuando el jefe indica confeccionar otro, el nuevo menú lo referencia (relación <c>Origina</c>,
/// <see cref="Menu.RegistrarComoReemplazo"/>). Un menú rechazado no se vuelve a revisar: se confecciona otro. Con
/// un solo dictamen por menú, «menú final» tiene una sola definición (el menú cuya validación está aprobada) y no
/// hace falta comparar fechas entre revisores. El par (revisor, menú) no sería una clave mínima.
/// </para>
/// <para>
/// <b>El rechazo se registra (decisión del equipo, 24/10; #155).</b> Revierte la decisión del 16/10 de borrar el
/// menú rechazado sin dejar registro, por dos razones. La consulta 3 del enunciado pide «las observaciones hechas
/// durante el proceso», que también son las de los rechazos. Y el profesor señaló que el jefe «puede indicar la
/// confección de otro [menú] bajo sus criterios» no estaba modelado (P1-5): con el rechazo registrado, es la
/// relación <c>Origina</c> desde el dictamen rechazado hacia el menú de reemplazo.
/// </para>
/// <para>
/// <b>Por qué vuelve <see cref="Aprobado"/>.</b> Mientras el rechazo no se registraba, el atributo valía siempre
/// <see langword="true"/> (la dependencia ∅ → aprobado, que viola la 3FN), y por eso se quitó en el #153. Con los
/// dos dictámenes registrados, toma los dos valores y depende de la clave, como cualquier otro atributo.
/// </para>
/// <para>
/// <b>No se modifica ni se borra (RS-15).</b> No tiene métodos para cambiarla, y la base rechaza el UPDATE y el
/// DELETE: si se pudiera borrar o cambiar, un menú ya asignado o servido volvería a estar pendiente, y un menú de
/// reemplazo quedaría colgado de una aprobación.
/// </para>
/// </remarks>
public sealed class Validacion : Entity<Guid>
{
    /// <param name="menuId">Identificador del menú dictaminado, que identifica la validación.</param>
    /// <param name="nutricionistaId">Identificador del nutricionista revisor.</param>
    /// <param name="fechaValidacion">Fecha y hora en que se emite el dictamen.</param>
    /// <param name="aprobado"><see langword="true"/> si el dictamen aprueba el menú; <see langword="false"/> si lo rechaza.</param>
    /// <param name="observaciones">Señalamientos del revisor, si existen.</param>
    internal Validacion(
        Guid menuId,
        Guid nutricionistaId,
        DateTime fechaValidacion,
        bool aprobado,
        string? observaciones = null)
        : base(menuId)
    {
        if (fechaValidacion.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("El instante de validación debe estar en UTC (DateTimeKind.Utc).", nameof(fechaValidacion));
        }

        NutricionistaId = nutricionistaId;
        FechaValidacion = fechaValidacion;
        Aprobado = aprobado;
        Observaciones = observaciones;
    }

    /// <summary>FK → Menú dictaminado, que es también la clave (RC-12, RK-13).</summary>
    public Guid MenuId => Id;

    /// <summary>FK → Nutricionista revisor.</summary>
    public Guid NutricionistaId { get; }

    /// <summary>Fecha y hora del dictamen.</summary>
    public DateTime FechaValidacion { get; }

    /// <summary>
    /// Indica si el dictamen aprobó el menú (<see langword="true"/>) o lo rechazó (<see langword="false"/>). Es
    /// obligatorio (RD-12).
    /// </summary>
    public bool Aprobado { get; }

    /// <summary>Observaciones del revisor; puede ser <see langword="null"/>.</summary>
    public string? Observaciones { get; }
}
