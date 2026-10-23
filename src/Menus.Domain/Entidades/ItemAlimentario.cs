using Menus.Domain.Abstractions;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Entidades;

/// <summary>
/// Elemento del banco de alimentos: un <see cref="Alimento"/> simple o un <see cref="Plato"/> compuesto.
/// Es la superclase de la generalización que reúne lo que los dos comparten: el nombre, la clasificación
/// por grupo nutricional, tipo de preparación y nivel calórico, y el nutricionista que lo describió.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por qué una generalización.</b> Alimento y plato tenían las mismas cuatro relaciones con las mismas
/// cardinalidades (<c>Pertenece</c>, <c>Corresponde</c>, <c>Tiene</c> y <c>Describe</c>), y además se
/// incluían en el menú por dos relaciones paralelas. El profesor lo señaló como estructura duplicada
/// (feedback del 19/10), y la orientación ya trata al plato como un alimento compuesto: «un alimento simple
/// puede describirse con pocos campos, mientras que un plato compuesto […] requiere una ficha
/// considerablemente más extensa». Con la superclase, cada relación existe una sola vez, el menú incluye
/// ítems y el paciente puede aceptar o rechazar un plato entero (<see cref="Consumo"/>).
/// </para>
/// <para>
/// <b>La especialización es total y disjunta:</b> todo ítem es un alimento o un plato, nunca los dos ni
/// ninguno. Por eso la clase es abstracta. Las subclases no tienen atributos propios: lo que las distingue
/// es la relación <c>Compone</c>, que va de un plato a un alimento simple (composición de un solo nivel,
/// R5-18). Por eso la persistencia usa una sola tabla con discriminador: es la traducción habitual de una
/// especialización así, y deja declarativas la totalidad y la disyunción.
/// </para>
/// <para>
/// El nombre es único entre todos los ítems, sin distinguir mayúsculas ni espacios sobrantes (R5-16): si un
/// plato es un alimento, «no existen alimentos duplicados» abarca a los dos. Lo comprueba
/// <see cref="Abstractions.Repositories.IItemAlimentarioRepository.ExisteNombreAsync"/> antes de registrar,
/// y la base lo garantiza con un índice único.
/// </para>
/// </remarks>
public abstract class ItemAlimentario : Entity<Guid>
{
    /// <summary>Valida y asigna los datos comunes a alimentos y platos.</summary>
    /// <param name="id">Identificador único del ítem.</param>
    /// <param name="nombre">Nombre del ítem (no vacío; único entre alimentos y platos, R5-16).</param>
    /// <param name="grupoNutricionalId">FK → GrupoNutricional (relación <c>Pertenece</c>).</param>
    /// <param name="tipoPreparacionId">
    /// FK → TipoPreparacion (relación <c>Corresponde</c>): uno de <see cref="TipoPreparacion.Todos"/> (R5-08).
    /// </param>
    /// <param name="nivelCaloricoId">
    /// FK → NivelCalorico (relación <c>Tiene</c>): uno de <see cref="NivelCalorico.Todos"/> (R5-09).
    /// </param>
    /// <param name="nutricionistaId">Nutricionista que describió el ítem (relación <c>Describe</c>).</param>
    /// <exception cref="ArgumentException">
    /// Si <paramref name="nombre"/> es nulo o solo espacios, o si el tipo de preparación o el nivel calórico no
    /// son del catálogo.
    /// </exception>
    protected ItemAlimentario(
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

    /// <summary>Nombre del ítem (no vacío; único entre alimentos y platos, R5-16).</summary>
    public string Nombre { get; }

    /// <summary>FK → GrupoNutricional (relación <c>Pertenece</c>).</summary>
    public Guid GrupoNutricionalId { get; }

    /// <summary>FK → TipoPreparacion (relación <c>Corresponde</c>).</summary>
    public Guid TipoPreparacionId { get; }

    /// <summary>FK → NivelCalorico (relación <c>Tiene</c>).</summary>
    public Guid NivelCaloricoId { get; }

    /// <summary>FK → Nutricionista que describió el ítem (relación <c>Describe</c>).</summary>
    public Guid NutricionistaId { get; }
}
