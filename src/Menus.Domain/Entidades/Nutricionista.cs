using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Representa a un nutricionista, responsable de describir platos y alimentos,
/// y de crear o validar menús de las dietas para las que está autorizado
/// (relación "Autoriza").
/// </summary>
public sealed class Nutricionista : Entity<Guid>
{
    private readonly List<NutricionistasDieta> _dietasAutorizadas = [];

    /// <param name="id">Identificador único del nutricionista.</param>
    /// <param name="nombre">Nombre del nutricionista (no puede estar vacío).</param>
    /// <param name="especialidadId">Especialidad del nutricionista.</param>
    /// <exception cref="ArgumentException">Si <paramref name="nombre"/> es nulo o solo espacios.</exception>
    public Nutricionista(Guid id, string nombre, Guid especialidadId) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
        EspecialidadId = especialidadId;
    }

    /// <summary>Nombre del nutricionista.</summary>
    public string Nombre { get; }

    /// <summary>FK → Especialidad.</summary>
    public Guid EspecialidadId { get; }

    /// <summary>
    /// Dietas para las que este nutricionista está autorizado a crear o validar menús (solo lectura).
    /// </summary>
    public IReadOnlyCollection<NutricionistasDieta> DietasAutorizadas => _dietasAutorizadas.AsReadOnly();

    /// <summary>
    /// Autoriza al nutricionista para la dieta indicada.
    /// </summary>
    /// <param name="dietaId">Identificador de la dieta a autorizar.</param>
    /// <returns>La autorización creada.</returns>
    /// <exception cref="InvalidOperationException">Si la dieta ya estaba autorizada.</exception>
    public NutricionistasDieta AutorizarDieta(Guid dietaId)
    {
        if (_dietasAutorizadas.Any(a => a.DietaId == dietaId))
        {
            throw new InvalidOperationException($"La dieta {dietaId} ya está autorizada para este nutricionista.");
        }

        var autorizacion = new NutricionistasDieta(Id, dietaId);
        _dietasAutorizadas.Add(autorizacion);
        return autorizacion;
    }
}
