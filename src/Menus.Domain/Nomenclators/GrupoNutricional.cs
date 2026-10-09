using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

/// <summary>
/// Categoría nutricional a la que pertenece un alimento o plato
/// (p. ej. Lácteos, Carnes, Cereales, Frutas, Vegetales).
/// Es un catálogo institucional abierto: la institución puede añadir o modificar
/// grupos sin cambiar el código. Se persiste en PostgreSQL con seed data inicial.
/// Se usa como criterio de cobertura en la generación automática de menús,
/// garantizando que el menú distribuya alimentos entre los grupos requeridos por la dieta.
/// </summary>
public sealed class GrupoNutricional : Entity<Guid>
{
    /// <param name="id">Identificador único del grupo.</param>
    /// <param name="nombre">Nombre descriptivo del grupo (no puede estar vacío).</param>
    /// <exception cref="ArgumentException">Si <paramref name="nombre"/> es nulo o solo espacios.</exception>
    public GrupoNutricional(Guid id, string nombre) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
    }

    /// <summary>Nombre descriptivo del grupo nutricional (p. ej. "Lácteos").</summary>
    public string Nombre { get; }
}
