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
    /// <param name="grupoNutricionalId">Grupo nutricional al que pertenece.</param>
    /// <param name="tipoPreparacion">Tipo de preparación (entrante, plato fuerte, postre, bebida).</param>
    /// <param name="nivelCalorico">Nivel calórico (bajo, medio, alto).</param>
    /// <param name="nutricionistaId">Nutricionista que describió el plato.</param>
    /// <exception cref="ArgumentException">Si <paramref name="nombre"/> es nulo o solo espacios.</exception>
    public Plato(
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

    /// <summary>Nombre del plato (único, no vacío).</summary>
    public string Nombre { get; }

    /// <summary>FK → GrupoNutricional.</summary>
    public Guid GrupoNutricionalId { get; }

    /// <summary>Tipo de preparación del plato.</summary>
    public TipoPreparacion TipoPreparacion { get; }

    /// <summary>Nivel calórico del plato.</summary>
    public NivelCalorico NivelCalorico { get; }

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
