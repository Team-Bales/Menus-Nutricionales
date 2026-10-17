using Menus.Domain.Abstractions;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Entidades;

/// <summary>
/// Representa un plato compuesto por múltiples alimentos (ingredientes),
/// con su propia clasificación nutricional independiente de sus ingredientes
/// y el nutricionista que lo describió.
/// </summary>
public sealed class Plato : Entity<Guid>
{
    private readonly List<PlatoIngrediente> _ingredientes = [];

    /// <param name="id">Identificador único del plato.</param>
    /// <param name="nombre">Nombre del plato (único, no vacío).</param>
    /// <param name="grupoNutricionalId">FK → GrupoNutricional.</param>
    /// <param name="tipoPreparacionId">FK → TipoPreparacion: uno de <see cref="TipoPreparacion.Todos"/> (R5-08).</param>
    /// <param name="nivelCaloricoId">FK → NivelCalorico: uno de <see cref="NivelCalorico.Todos"/> (R5-09).</param>
    /// <param name="nutricionistaId">Nutricionista que describió el plato.</param>
    /// <exception cref="ArgumentException">
    /// Si <paramref name="nombre"/> es nulo o solo espacios, o si el tipo de preparación o el nivel calórico no
    /// son del catálogo.
    /// </exception>
    public Plato(
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

    /// <summary>Nombre del plato (único, no vacío).</summary>
    public string Nombre { get; }

    /// <summary>FK → GrupoNutricional.</summary>
    public Guid GrupoNutricionalId { get; }

    /// <summary>FK → TipoPreparacion.</summary>
    public Guid TipoPreparacionId { get; }

    /// <summary>FK → NivelCalorico.</summary>
    public Guid NivelCaloricoId { get; }

    /// <summary>FK → Nutricionista que describió el plato.</summary>
    public Guid NutricionistaId { get; }

    /// <summary>
    /// Colección de ingredientes del plato (solo lectura).
    /// Un plato debe tener al menos un ingrediente (validado en la capa de aplicación / Unit of Work).
    /// </summary>
    public IReadOnlyCollection<PlatoIngrediente> Ingredientes => _ingredientes.AsReadOnly();

    /// <summary>
    /// Agrega un ingrediente al plato.
    /// </summary>
    /// <param name="alimentoId">Identificador del alimento a agregar.</param>
    /// <param name="cantidad">Cantidad del alimento en el plato (> 0).</param>
    /// <returns>El ingrediente agregado.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="cantidad"/> no es positiva.</exception>
    /// <exception cref="InvalidOperationException">Si el alimento ya está en la composición del plato.</exception>
    public PlatoIngrediente AgregarIngrediente(Guid alimentoId, decimal cantidad)
    {
        if (cantidad <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser estrictamente positiva.");
        }

        if (_ingredientes.Any(i => i.AlimentoId == alimentoId))
        {
            throw new InvalidOperationException($"El alimento {alimentoId} ya forma parte de la composición de este plato.");
        }

        var ingrediente = new PlatoIngrediente(Id, alimentoId, cantidad);
        _ingredientes.Add(ingrediente);
        return ingrediente;
    }
}
