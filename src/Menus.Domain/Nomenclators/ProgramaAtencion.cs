using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

/// <summary>
/// Programa institucional de atención al que está vinculada una dieta
/// (p. ej. "Nutrición Pediátrica", "Oncología", "Geriatría").
/// Cada dieta pertenece a exactamente un programa de atención,
/// lo que permite segmentar los menús y los reportes por área clínica.
/// Se persiste en PostgreSQL con seed data inicial.
/// </summary>
public sealed class ProgramaAtencion : Entity<Guid>
{
    /// <param name="id">Identificador único del programa.</param>
    /// <param name="nombre">Nombre descriptivo del programa (no puede estar vacío).</param>
    /// <exception cref="ArgumentException">Si <paramref name="nombre"/> es nulo o solo espacios.</exception>
    public ProgramaAtencion(Guid id, string nombre) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
    }

    /// <summary>Nombre descriptivo del programa de atención (p. ej. "Nutrición Pediátrica").</summary>
    public string Nombre { get; }
}
