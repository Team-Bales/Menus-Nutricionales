using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

/// <summary>
/// Condición dietética que limita el uso de ciertos alimentos en una dieta
/// (p. ej. "Sin gluten", "Hiposódica", "Sin lactosa", "Vegetariana").
/// Una dieta puede tener varias restricciones activas simultáneamente.
/// Durante la generación automática de menús, solo se incluyen alimentos
/// que no infringen ninguna de las restricciones de la dieta objetivo.
/// Se persiste en PostgreSQL con seed data inicial.
/// </summary>
public sealed class RestriccionAlimentaria : Entity<Guid>
{
    /// <param name="id">Identificador único de la restricción.</param>
    /// <param name="nombre">Nombre descriptivo de la restricción (no puede estar vacío).</param>
    /// <exception cref="ArgumentException">Si <paramref name="nombre"/> es nulo o solo espacios.</exception>
    public RestriccionAlimentaria(Guid id, string nombre) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
    }

    /// <summary>Nombre descriptivo de la restricción alimentaria (p. ej. "Sin gluten").</summary>
    public string Nombre { get; }
}
