using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Representa un alimento individual del sistema, con su clasificación nutricional
/// y el nutricionista que lo describió.
/// </summary>
public sealed class Alimento : Entity<Guid>
{
    /// <param name="id">Identificador único del alimento.</param>
    /// <param name="nombre">Nombre del alimento (único, no vacío).</param>
    /// <param name="grupoNutricionalId">FK → GrupoNutricional.</param>
    /// <param name="tipoPreparacionId">FK → TipoPreparacion.</param>
    /// <param name="nivelCaloricoId">FK → NivelCalorico.</param>
    /// <param name="nutricionistaId">Nutricionista que describió el alimento.</param>
    /// <exception cref="ArgumentException">Si <paramref name="nombre"/> es nulo o solo espacios.</exception>
    public Alimento(
        Guid id,
        string nombre,
        Guid grupoNutricionalId,
        Guid tipoPreparacionId,
        Guid nivelCaloricoId,
        Guid nutricionistaId) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
        GrupoNutricionalId = grupoNutricionalId;
        TipoPreparacionId = tipoPreparacionId;
        NivelCaloricoId = nivelCaloricoId;
        NutricionistaId = nutricionistaId;
    }

    /// <summary>Nombre del alimento (único, no vacío).</summary>
    public string Nombre { get; }

    /// <summary>FK → GrupoNutricional.</summary>
    public Guid GrupoNutricionalId { get; }

    /// <summary>FK → TipoPreparacion.</summary>
    public Guid TipoPreparacionId { get; }

    /// <summary>FK → NivelCalorico.</summary>
    public Guid NivelCaloricoId { get; }

    /// <summary>FK → Nutricionista que describió el alimento.</summary>
    public Guid NutricionistaId { get; }
}
