using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Representa a un nutricionista, responsable de describir platos y alimentos,
/// y de crear o validar menús de las dietas para las que está autorizado
/// (relación "Autoriza"). Validar exige además ocupar el cargo de jefe de nutrición
/// (<see cref="EsJefe"/>, RS-03).
/// </summary>
/// <remarks>
/// El jefe de nutrición es un cargo, no una especialidad ni una subclase: lo registra
/// <see cref="EsJefe"/> (RD-18, informe §2.1). Puede haber más de un jefe a la vez, porque
/// la orientación no fija cuántos, así que el modelo no impone que sea único.
/// </remarks>
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
    /// Si el nutricionista ocupa el cargo de jefe de nutrición (RD-18). Empieza en falso.
    /// </summary>
    /// <remarks>
    /// Solo un jefe puede registrar dictámenes (RS-03). La base lo comprueba al registrar la
    /// validación, no después: retirar el cargo no invalida los dictámenes ya registrados.
    /// </remarks>
    public bool EsJefe { get; private set; }

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

    /// <summary>
    /// Designa al nutricionista como jefe de nutrición. Si ya lo es, no cambia nada.
    /// </summary>
    /// <remarks>
    /// La designación la gestiona la administración del sistema (RS-03): restringir quién puede
    /// llamarla corresponde a los permisos de la aplicación, no a la entidad.
    /// </remarks>
    public void DesignarJefe() => EsJefe = true;

    /// <summary>
    /// Retira al nutricionista del cargo de jefe de nutrición. Si no lo era, no cambia nada.
    /// </summary>
    /// <remarks>
    /// Los dictámenes que registró mientras era jefe siguen siendo válidos (RS-03).
    /// </remarks>
    public void RetirarJefe() => EsJefe = false;
}
