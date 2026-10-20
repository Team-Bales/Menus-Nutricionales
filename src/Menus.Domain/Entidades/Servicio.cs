using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Cada vez que un menú asignado se le sirve al paciente: entidad débil de la agregación <c>Asignación</c>,
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
/// <b>Por qué la fecha se trunca al segundo.</b> Forma parte de la clave. .NET guarda fracciones de 100 ns y
/// PostgreSQL de 1 µs: sin truncar, una fecha leída de la base podría no coincidir con la que el dominio tiene
/// en memoria, y el mismo servicio parecería otro. Con el segundo alcanza para distinguir dos servicios reales.
/// </para>
/// <para>
/// La valoración nutricional sigue evaluando la asignación, no el servicio: valora el estado del paciente ante el
/// menú recibido, no ante una comida en particular.
/// </para>
/// </remarks>
public sealed class Servicio : Entity<(Guid MenuId, Guid PacienteId, DateTime FechaServicio)>
{
    private readonly List<Consumo> _consumos = [];

    /// <param name="menuId">Identificador del menú servido.</param>
    /// <param name="pacienteId">Identificador del paciente al que se sirvió.</param>
    /// <param name="fechaServicio">Instante del servicio, en UTC; se trunca al segundo (<see cref="Normalizar"/>).</param>
    internal Servicio(Guid menuId, Guid pacienteId, DateTime fechaServicio)
        : base((menuId, pacienteId, Normalizar(fechaServicio))) { }

    /// <summary>FK → Menú de la asignación (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>FK → Paciente de la asignación (parte de la clave compuesta).</summary>
    public Guid PacienteId => Id.PacienteId;

    /// <summary>Instante del servicio, en UTC y truncado al segundo (llave parcial de la entidad débil).</summary>
    public DateTime FechaServicio => Id.FechaServicio;

    /// <summary>Consumos registrados en este servicio (solo lectura).</summary>
    public IReadOnlyCollection<Consumo> Consumos => _consumos.AsReadOnly();

    /// <summary>Trunca un instante al segundo, conservando que está en UTC.</summary>
    /// <param name="fecha">Instante a normalizar.</param>
    /// <returns>El mismo instante sin fracciones de segundo.</returns>
    internal static DateTime Normalizar(DateTime fecha)
        => new(fecha.Ticks - (fecha.Ticks % TimeSpan.TicksPerSecond), fecha.Kind);

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
