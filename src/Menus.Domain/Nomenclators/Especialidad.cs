using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

/// <summary>
/// Especialidad médica o nutricional de un nutricionista
/// (p. ej. "Dietética Clínica", "Nutrición Deportiva", "Nutrición Pediátrica").
/// Permite clasificar y filtrar nutricionistas por área de expertise.
/// Se persiste en PostgreSQL con seed data inicial.
/// </summary>
public sealed class Especialidad : Entity<Guid>
{
    /// <param name="id">Identificador único de la especialidad.</param>
    /// <param name="nombre">Nombre descriptivo de la especialidad (no puede estar vacío).</param>
    /// <exception cref="ArgumentException">Si <paramref name="nombre"/> es nulo o solo espacios.</exception>
    public Especialidad(Guid id, string nombre) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
    }

    /// <summary>Nombre descriptivo de la especialidad (p. ej. "Dietética Clínica").</summary>
    public string Nombre { get; }
}
