using Menus.Domain.Abstractions;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Entidades;

/// <summary>
/// Representa un alimento individual del sistema, con su clasificación nutricional
/// y el nutricionista que lo describió.
/// </summary>
public sealed class Alimento : Entity<Guid>
{
    /// <param name="id">Identificador único del alimento.</param>
    /// <param name="nombre">Nombre del alimento (único, no vacío).</param>
    /// <param name="grupoNutricionalId">Grupo nutricional al que pertenece.</param>
    /// <param name="tipoPreparacion">Tipo de preparación (entrante, plato fuerte, postre, bebida).</param>
    /// <param name="nivelCalorico">Nivel calórico (bajo, medio, alto).</param>
    /// <param name="nutricionistaId">Nutricionista que describió el alimento.</param>
    /// <exception cref="ArgumentException">Si <paramref name="nombre"/> es nulo o solo espacios.</exception>
    public Alimento(
        Guid id,
        string nombre,
        Guid grupoNutricionalId,
        TipoPreparacion tipoPreparacion,
        NivelCalorico nivelCalorico,
        Guid nutricionistaId) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
        GrupoNutricionalId = grupoNutricionalId;
        TipoPreparacion = tipoPreparacion;
        NivelCalorico = nivelCalorico;
        NutricionistaId = nutricionistaId;
    }

    /// <summary>Nombre del alimento (único, no vacío).</summary>
    public string Nombre { get; }

    /// <summary>FK → GrupoNutricional.</summary>
    public Guid GrupoNutricionalId { get; }

    /// <summary>Tipo de preparación del alimento.</summary>
    public TipoPreparacion TipoPreparacion { get; }

    /// <summary>Nivel calórico del alimento.</summary>
    public NivelCalorico NivelCalorico { get; }

    /// <summary>FK → Nutricionista que describió el alimento.</summary>
    public Guid NutricionistaId { get; }
}
