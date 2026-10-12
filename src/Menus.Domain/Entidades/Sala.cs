using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Representa una sala o unidad asistencial a la que pertenecen los pacientes
/// (relación "Pertenece").
/// </summary>
public sealed class Sala : Entity<Guid>
{
    /// <param name="id">Identificador único de la sala.</param>
    /// <param name="nombre">Nombre descriptivo de la sala (no puede estar vacío).</param>
    /// <exception cref="ArgumentException">Si <paramref name="nombre"/> es nulo o solo espacios.</exception>
    public Sala(Guid id, string nombre) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
    }

    /// <summary>Nombre descriptivo de la sala.</summary>
    public string Nombre { get; }
}
