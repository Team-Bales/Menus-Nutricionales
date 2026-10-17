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
    /// <param name="grupoNutricionalId">FK → GrupoNutricional.</param>
    /// <param name="tipoPreparacionId">FK → TipoPreparacion: uno de <see cref="TipoPreparacion.Todos"/> (R5-08).</param>
    /// <param name="nivelCaloricoId">FK → NivelCalorico: uno de <see cref="NivelCalorico.Todos"/> (R5-09).</param>
    /// <param name="nutricionistaId">Nutricionista que describió el alimento.</param>
    /// <exception cref="ArgumentException">
    /// Si <paramref name="nombre"/> es nulo o solo espacios, o si el tipo de preparación o el nivel calórico no
    /// son del catálogo.
    /// </exception>
    public Alimento(
        Guid id,
        string nombre,
        Guid grupoNutricionalId,
        Guid tipoPreparacionId,
        Guid nivelCaloricoId,
        Guid nutricionistaId) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        if (!TipoPreparacion.Existe(tipoPreparacionId))
        {
            throw new ArgumentException($"{tipoPreparacionId} no es un tipo de preparación del catálogo.", nameof(tipoPreparacionId));
        }

        if (!NivelCalorico.Existe(nivelCaloricoId))
        {
            throw new ArgumentException($"{nivelCaloricoId} no es un nivel calórico del catálogo.", nameof(nivelCaloricoId));
        }

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
