using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Destinatario de los menús nutricionales. Pertenece a una sala de hospitalización
/// y puede estar inscrito en una o más dietas activas.
/// </summary>
public sealed class Paciente : Entity<Guid>
{
    private readonly List<PacienteDieta> _dietas = [];

    /// <param name="id">Identificador único del paciente.</param>
    /// <param name="nombre">Nombre del paciente (no vacío).</param>
    /// <param name="edad">Edad en años enteros (estrictamente positiva).</param>
    /// <param name="salaId">Sala de hospitalización a la que pertenece.</param>
    /// <exception cref="ArgumentException">Si <paramref name="nombre"/> es nulo o solo espacios.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="edad"/> es menor o igual a cero.</exception>
    public Paciente(Guid id, string nombre, int edad, Guid salaId) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        if (edad <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(edad), "La edad debe ser un entero estrictamente positivo.");
        }

        Nombre = nombre;
        Edad = edad;
        SalaId = salaId;
    }

    /// <summary>Nombre del paciente (no vacío).</summary>
    public string Nombre { get; }

    /// <summary>Edad en años enteros (> 0).</summary>
    public int Edad { get; }

    /// <summary>FK → Sala de hospitalización a la que pertenece.</summary>
    public Guid SalaId { get; }

    /// <summary>
    /// Dietas en las que el paciente está inscrito (solo lectura).
    /// </summary>
    public IReadOnlyCollection<PacienteDieta> Dietas => _dietas.AsReadOnly();

    /// <summary>
    /// Inscribe al paciente en una dieta. Lanza excepción si ya está inscrito en ella.
    /// </summary>
    /// <param name="dietaId">Identificador de la dieta.</param>
    /// <returns>La inscripción creada.</returns>
    /// <exception cref="InvalidOperationException">Si el paciente ya está inscrito en esa dieta.</exception>
    public PacienteDieta InscribirDieta(Guid dietaId)
    {
        if (_dietas.Any(d => d.DietaId == dietaId))
        {
            throw new InvalidOperationException($"El paciente ya está inscrito en la dieta {dietaId}.");
        }

        var inscripcion = new PacienteDieta(Id, dietaId);
        _dietas.Add(inscripcion);
        return inscripcion;
    }
}
