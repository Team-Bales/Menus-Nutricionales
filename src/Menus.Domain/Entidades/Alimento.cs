namespace Menus.Domain.Entidades;

/// <summary>
/// Alimento simple del banco: la subclase de <see cref="ItemAlimentario"/> que puede formar parte de la
/// composición de un plato.
/// </summary>
/// <remarks>
/// No tiene atributos propios: hereda el nombre, la clasificación y el nutricionista que lo describió. Lo que
/// lo distingue de un <see cref="Plato"/> es su papel en la relación <c>Compone</c>: solo un alimento simple es
/// ingrediente de un plato (R5-18).
/// </remarks>
public sealed class Alimento : ItemAlimentario
{
    /// <param name="id">Identificador único del alimento.</param>
    /// <param name="nombre">Nombre del alimento (no vacío; único entre alimentos y platos, R5-16).</param>
    /// <param name="grupoNutricionalId">FK → GrupoNutricional.</param>
    /// <param name="tipoPreparacionId">FK → TipoPreparacion: uno de <see cref="Nomenclators.TipoPreparacion.Todos"/> (R5-08).</param>
    /// <param name="nivelCaloricoId">FK → NivelCalorico: uno de <see cref="Nomenclators.NivelCalorico.Todos"/> (R5-09).</param>
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
        Guid nutricionistaId)
        : base(id, nombre, grupoNutricionalId, tipoPreparacionId, nivelCaloricoId, nutricionistaId)
    {
    }
}
