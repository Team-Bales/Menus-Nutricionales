using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

/// <summary>
/// Resultado de una valoración nutricional. Es un catálogo cerrado de exactamente cinco categorías,
/// de <see cref="MuyMala"/> (1) a <see cref="Excelente"/> (5), con nombre y valor únicos (R7-19).
/// El <see cref="Valor"/> permite promediar los resultados (consulta 6).
/// </summary>
/// <remarks>
/// A diferencia de los otros catálogos cerrados, que son enums (ADR-003), es una entidad: en el MERX
/// tiene identificador, nombre y valor propios, y las valoraciones lo referencian por su identificador
/// (ver ADR-006). El constructor es privado, así que no existen más categorías que estas cinco. Sus
/// identificadores son fijos para que el seed de la base use los mismos.
/// </remarks>
public sealed class ResultadoNutricional : Entity<Guid>
{
    /// <summary>Resultado «Muy mala», con valor 1.</summary>
    public static readonly ResultadoNutricional MuyMala = new(new Guid("e89977e7-eeb6-4d32-96e2-096ace535bc8"), "Muy mala", 1);

    /// <summary>Resultado «Mala», con valor 2.</summary>
    public static readonly ResultadoNutricional Mala = new(new Guid("4077627e-08b1-404a-aeb5-bb64a90538ba"), "Mala", 2);

    /// <summary>Resultado «Normal», con valor 3.</summary>
    public static readonly ResultadoNutricional Normal = new(new Guid("023f26c4-50c4-4e20-8c24-7d27b341dfd6"), "Normal", 3);

    /// <summary>Resultado «Buena», con valor 4.</summary>
    public static readonly ResultadoNutricional Buena = new(new Guid("c0cf2cb9-45bb-4a30-ab00-0c6a8bfc5ca1"), "Buena", 4);

    /// <summary>Resultado «Excelente», con valor 5.</summary>
    public static readonly ResultadoNutricional Excelente = new(new Guid("138dec6d-8202-4b54-831c-e6fd21508695"), "Excelente", 5);

    /// <summary>Las cinco categorías, ordenadas por valor de menor a mayor.</summary>
    public static IReadOnlyList<ResultadoNutricional> Todos { get; } = [MuyMala, Mala, Normal, Buena, Excelente];

    private ResultadoNutricional(Guid id, string nombre, int valor) : base(id)
    {
        Nombre = nombre;
        Valor = valor;
    }

    /// <summary>Nombre de la categoría (p. ej. «Muy mala»).</summary>
    public string Nombre { get; }

    /// <summary>Valor numérico de la categoría, de 1 a 5.</summary>
    public int Valor { get; }

    /// <summary>Devuelve la categoría que corresponde a un valor.</summary>
    /// <param name="valor">Valor de la categoría, de 1 a 5.</param>
    /// <returns>La categoría con ese valor.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="valor"/> no está entre 1 y 5.</exception>
    public static ResultadoNutricional DesdeValor(int valor) =>
        Todos.SingleOrDefault(r => r.Valor == valor)
        ?? throw new ArgumentOutOfRangeException(nameof(valor), "El resultado nutricional debe tener un valor entre 1 y 5.");
}
