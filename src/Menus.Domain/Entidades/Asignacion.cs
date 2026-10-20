using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Asignación de un menú a un paciente (agregación <c>Asignación</c> / relación <c>Asigna</c>).
/// Un menú puede asignarse a varios pacientes, pero una sola vez a cada uno (R7-05).
/// De la asignación dependen los servicios del menú al paciente (con sus consumos) y las valoraciones
/// (R7-11, R7-12). Solo la crea <see cref="Menu"/>.
/// </summary>
/// <remarks>
/// En el modelo revisado (feedback del 19/10) es la única agregación, porque es la única relación que participa
/// en otras: <c>Sirve</c> (identifica a <see cref="Servicio"/>) y <c>Evalúa</c> (la valoración). Las demás
/// relaciones con atributos los llevan en el rombo.
/// </remarks>
public sealed class Asignacion : Entity<(Guid MenuId, Guid PacienteId)>
{
    private readonly List<Servicio> _servicios = [];

    /// <param name="menuId">Identificador del menú asignado.</param>
    /// <param name="pacienteId">Identificador del paciente que recibe el menú.</param>
    internal Asignacion(Guid menuId, Guid pacienteId) : base((menuId, pacienteId)) { }

    /// <summary>FK → Menu (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>FK → Paciente (parte de la clave compuesta).</summary>
    public Guid PacienteId => Id.PacienteId;

    /// <summary>Servicios del menú a este paciente, cada uno con sus consumos (solo lectura).</summary>
    public IReadOnlyCollection<Servicio> Servicios => _servicios.AsReadOnly();

    /// <summary>Registra un servicio del menú al paciente. Las reglas de la fecha las comprueba <see cref="Menu"/>.</summary>
    /// <param name="fechaServicio">Día del servicio, en el calendario UTC.</param>
    /// <returns>El servicio registrado.</returns>
    /// <exception cref="InvalidOperationException">Si ya hay un servicio ese día.</exception>
    internal Servicio RegistrarServicio(DateOnly fechaServicio)
    {
        if (_servicios.Any(s => s.FechaServicio == fechaServicio))
        {
            throw new InvalidOperationException($"Este menú ya se le sirvió al paciente el {fechaServicio:yyyy-MM-dd}.");
        }

        var servicio = new Servicio(MenuId, PacienteId, fechaServicio);
        _servicios.Add(servicio);
        return servicio;
    }
}
