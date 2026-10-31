namespace Menus.Domain.Entidades;

/// <summary>
/// Plato compuesto del banco: la subclase de <see cref="ItemAlimentario"/> que se compone de alimentos simples
/// (relación <c>Compone</c>), con su propia clasificación nutricional, independiente de la de sus ingredientes.
/// </summary>
/// <remarks>
/// La composición tiene un solo nivel: los ingredientes son alimentos simples, nunca otros platos (RS-12).
/// El dominio solo conoce el identificador del ingrediente, así que comprueba lo que puede (que el plato no se
/// componga de sí mismo); que el ingrediente sea de clase <see cref="Alimento"/> lo garantiza la base.
/// </remarks>
public sealed class Plato : ItemAlimentario
{
    private readonly List<PlatoIngrediente> _ingredientes = [];

    /// <param name="id">Identificador único del plato.</param>
    /// <param name="nombre">Nombre del plato (no vacío; único entre alimentos y platos, RC-01).</param>
    /// <param name="grupoNutricionalId">FK → GrupoNutricional.</param>
    /// <param name="tipoPreparacionId">FK → TipoPreparacion: uno de <see cref="Nomenclators.TipoPreparacion.Todos"/> (RD-01).</param>
    /// <param name="nivelCaloricoId">FK → NivelCalorico: uno de <see cref="Nomenclators.NivelCalorico.Todos"/> (RD-02).</param>
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
        Guid nutricionistaId)
        : base(id, nombre, grupoNutricionalId, tipoPreparacionId, nivelCaloricoId, nutricionistaId)
    {
    }

    /// <summary>
    /// Colección de ingredientes del plato (solo lectura).
    /// Un plato debe tener al menos un ingrediente (RK-03): se registra con su composición en una misma unidad
    /// de trabajo, y la base lo comprueba al confirmar.
    /// </summary>
    public IReadOnlyCollection<PlatoIngrediente> Ingredientes => _ingredientes.AsReadOnly();

    /// <summary>
    /// Agrega un ingrediente al plato.
    /// </summary>
    /// <param name="alimentoId">Identificador del alimento simple a agregar.</param>
    /// <param name="cantidad">Cantidad del alimento en el plato (&gt; 0, RD-05).</param>
    /// <returns>El ingrediente agregado.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="cantidad"/> no es positiva.</exception>
    /// <exception cref="InvalidOperationException">
    /// Si el ingrediente es el propio plato (RS-12) o si el alimento ya está en la composición del plato.
    /// </exception>
    public PlatoIngrediente AgregarIngrediente(Guid alimentoId, decimal cantidad)
    {
        if (alimentoId == Id)
        {
            throw new InvalidOperationException("Un plato no puede ser ingrediente de sí mismo: sus ingredientes son alimentos simples.");
        }

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

    /// <summary>Quita un ingrediente de la composición del plato.</summary>
    /// <remarks>
    /// No se quita el último: todo plato se compone de al menos un alimento (RK-03), y la base lo comprueba
    /// igual al confirmar. Si el plato tiene ficha nutricional, el caso de uso la actualiza, porque sus
    /// ingredientes tienen que coincidir con la composición (RM-06).
    /// </remarks>
    /// <param name="alimentoId">Identificador del alimento que se quita.</param>
    /// <exception cref="InvalidOperationException">
    /// Si el alimento no está en la composición, o si es el único ingrediente del plato (RK-03).
    /// </exception>
    public void QuitarIngrediente(Guid alimentoId)
    {
        var ingrediente = _ingredientes.SingleOrDefault(i => i.AlimentoId == alimentoId)
            ?? throw new InvalidOperationException($"El alimento {alimentoId} no forma parte de la composición de este plato.");

        if (_ingredientes.Count == 1)
        {
            throw new InvalidOperationException("Un plato se compone de al menos un alimento (RK-03): no se quita su único ingrediente.");
        }

        _ingredientes.Remove(ingrediente);
    }
}
