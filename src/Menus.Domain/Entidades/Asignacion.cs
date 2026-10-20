using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Asignación de un menú a un paciente (agregación <c>Asignación</c> / relación <c>Asigna</c>).
/// Un menú puede asignarse a varios pacientes, pero una sola vez a cada uno (R7-05).
/// De la asignación dependen los consumos y las valoraciones del paciente ante ese menú (R7-11, R7-12).
/// Solo la crea <see cref="Menu"/>.
/// </summary>
public sealed class Asignacion : Entity<(Guid MenuId, Guid PacienteId)>
{
    private readonly List<Consumo> _consumos = [];

    /// <param name="menuId">Identificador del menú asignado.</param>
    /// <param name="pacienteId">Identificador del paciente que recibe el menú.</param>
    internal Asignacion(Guid menuId, Guid pacienteId) : base((menuId, pacienteId)) { }

    /// <summary>FK → Menu (parte de la clave compuesta).</summary>
    public Guid MenuId => Id.MenuId;

    /// <summary>FK → Paciente (parte de la clave compuesta).</summary>
    public Guid PacienteId => Id.PacienteId;

    /// <summary>Consumos registrados para la asignación (solo lectura).</summary>
    public IReadOnlyCollection<Consumo> Consumos => _consumos.AsReadOnly();

    /// <summary>Registra la aceptación o el rechazo de un alimento o un plato de este menú asignado.</summary>
    /// <param name="itemId">Identificador del alimento o plato consumido.</param>
    /// <param name="aceptacion"><see langword="true"/> si el paciente lo aceptó; de lo contrario, <see langword="false"/>.</param>
    /// <returns>El consumo registrado.</returns>
    /// <exception cref="InvalidOperationException">Si ya existe un consumo de ese ítem para esta asignación.</exception>
    public Consumo RegistrarConsumo(Guid itemId, bool aceptacion)
    {
        if (_consumos.Any(c => c.ItemId == itemId))
        {
            throw new InvalidOperationException($"El ítem {itemId} ya tiene un consumo registrado para esta asignación.");
        }

        var consumo = new Consumo(MenuId, PacienteId, itemId, aceptacion);
        _consumos.Add(consumo);
        return consumo;
    }
}
