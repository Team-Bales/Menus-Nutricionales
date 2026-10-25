using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Cada día en que un menú asignado se le sirve al paciente: entidad débil de la agregación <c>Asignación</c>,
/// identificada dentro de ella por <see cref="FechaServicio"/> (relación identificadora <c>Sirve</c>).
/// De cada servicio dependen los consumos registrados en él. Solo lo crea <see cref="Menu.RegistrarServicio"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por qué existe.</b> Antes el consumo colgaba de la asignación, con clave (menú, paciente, alimento): si el
/// mismo menú asignado se servía varios días del plan, solo se podía registrar un consumo por alimento. Eso
/// chocaba con la orientación, que pide «el almacenamiento de cada consumo registrado ante un menú servido», y el
/// profesor lo señaló (feedback del 19/10): el menú servido no estaba modelado como un hecho.
/// </para>
/// <para>
/// <b>Por qué es una entidad débil de la asignación.</b> Un servicio no existe sin la asignación que lo
/// autoriza, y la asignación sigue siendo única por menú y paciente (R7-05: no se asigna dos veces); lo que se
/// repite es el servicio. Su identidad es la de la asignación más la fecha.
/// </para>
/// <para>
/// <b>Por qué la llave parcial es el día, sin hora.</b> El caso que señaló el profesor es el mismo menú servido
/// «varios días del plan», así que un servicio es un día de una asignación. Con la fecha sola, el mismo servicio
/// no puede registrarse dos veces por una diferencia de segundos, y la identidad no depende de la precisión con
/// que .NET y PostgreSQL guardan la hora (revisión del 19/10; antes era un instante truncado al segundo). El día se
/// expresa en el calendario UTC, como el resto de las fechas del sistema. Si el equipo decidiera que un menú
/// corresponde a una sola comida del día, la clave sumaría el turno.
/// </para>
/// <para>
/// <b>Lo que deliberadamente no se restringe.</b> Que el paciente esté inscrito en la dieta del menú (R6-23) se
/// comprueba al asignar, no en cada servicio. Si después deja la dieta, la asignación queda como historial, y
/// seguir sirviéndole ese menú o no es una decisión clínica que corresponde a la capa de aplicación.
/// </para>
/// <para>
/// La valoración nutricional sigue evaluando la asignación, no el servicio: valora el estado del paciente ante el
/// menú recibido, no ante un día en particular.
/// </para>
/// </remarks>
public sealed class Servicio : Entity<(Guid MenuId, Guid PacienteId, DateOnly FechaServicio)>
{
    private readonly List<Consumo> _consumos = [];

    /// <param name="menuId">Identificador del menú servido.</param>
    /// <param name="pacienteId">Identificador del paciente al que se sirvió.</param>
    /// <param name="fechaServicio">Día del servicio, en el calendario UTC.</param>
    internal Servicio(Guid menuId, Guid pacienteId, DateOnly fechaServicio)
        : base((menuId, pacienteId, fechaServicio)) { }

    /// <summary>FK → Menú de la asignación (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>FK → Paciente de la asignación (parte de la clave compuesta).</summary>
    public Guid PacienteId => Id.PacienteId;

    /// <summary>Día del servicio, en el calendario UTC (llave parcial de la entidad débil).</summary>
    public DateOnly FechaServicio => Id.FechaServicio;

    /// <summary>Consumos registrados en este servicio (solo lectura).</summary>
    public IReadOnlyCollection<Consumo> Consumos => _consumos.AsReadOnly();

    /// <summary>Registra la aceptación o el rechazo de un ítem del menú en este servicio.</summary>
    /// <param name="itemId">Identificador del alimento o plato consumido.</param>
    /// <param name="aceptacion"><see langword="true"/> si el paciente lo aceptó; de lo contrario, <see langword="false"/>.</param>
    /// <returns>El consumo registrado.</returns>
    /// <exception cref="InvalidOperationException">Si el ítem ya tiene un consumo registrado en este servicio.</exception>
    internal Consumo RegistrarConsumo(Guid itemId, bool aceptacion)
    {
        if (_consumos.Any(c => c.ItemId == itemId))
        {
            throw new InvalidOperationException($"El ítem {itemId} ya tiene un consumo registrado en este servicio.");
        }

        var consumo = new Consumo(MenuId, PacienteId, FechaServicio, itemId, aceptacion);
        _consumos.Add(consumo);
        return consumo;
    }
}
