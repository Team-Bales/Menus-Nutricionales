using Menus.Domain.Abstractions;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Entidades;

/// <summary>
/// Dieta que define los grupos nutricionales a cubrir y las restricciones alimentarias a respetar.
/// </summary>
public sealed class Dieta : Entity<Guid>
{
    private readonly List<DietaGrupoNutricional> _gruposNutricionales = [];
    private readonly List<DietaRestriccion> _restricciones = [];

    /// <param name="id">Identificador único de la dieta.</param>
    /// <param name="nombre">Nombre descriptivo de la dieta.</param>
    /// <param name="programaAtencionId">Identificador del programa de atención al que pertenece.</param>
    /// <exception cref="ArgumentException">Si <paramref name="nombre"/> es nulo o vacío.</exception>
    public Dieta(Guid id, string nombre, Guid programaAtencionId) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
        ProgramaAtencionId = programaAtencionId;
    }

    /// <summary>Nombre descriptivo de la dieta.</summary>
    public string Nombre { get; }

    /// <summary>FK → Programa de atención al que pertenece la dieta.</summary>
    public Guid ProgramaAtencionId { get; }

    /// <summary>Grupos nutricionales que cubre la dieta (requeridos o restringidos).</summary>
    public IReadOnlyCollection<DietaGrupoNutricional> GruposNutricionales => _gruposNutricionales.AsReadOnly();

    /// <summary>Restricciones alimentarias que aplica la dieta.</summary>
    public IReadOnlyCollection<DietaRestriccion> Restricciones => _restricciones.AsReadOnly();

    /// <summary>Registra que la dieta cubre un grupo nutricional con el tipo indicado.</summary>
    /// <param name="grupoNutricionalId">Identificador del grupo nutricional.</param>
    /// <param name="tipo">Tipo de cobertura: requerido o restringido.</param>
    /// <returns>La cobertura registrada.</returns>
    /// <exception cref="InvalidOperationException">Si el grupo ya está registrado en la dieta.</exception>
    public DietaGrupoNutricional AgregarGrupo(Guid grupoNutricionalId, TipoCobertura tipo)
    {
        if (_gruposNutricionales.Any(g => g.GrupoNutricionalId == grupoNutricionalId))
        {
            throw new InvalidOperationException($"La dieta ya cubre el grupo nutricional {grupoNutricionalId}.");
        }

        var grupo = new DietaGrupoNutricional(Id, grupoNutricionalId, tipo);
        _gruposNutricionales.Add(grupo);
        return grupo;
    }

    /// <summary>Registra una restricción alimentaria en la dieta.</summary>
    /// <param name="restriccionAlimentariaId">Identificador de la restricción alimentaria.</param>
    /// <returns>La restricción registrada.</returns>
    /// <exception cref="InvalidOperationException">Si la restricción ya está registrada en la dieta.</exception>
    public DietaRestriccion AgregarRestriccion(Guid restriccionAlimentariaId)
    {
        if (_restricciones.Any(r => r.RestriccionAlimentariaId == restriccionAlimentariaId))
        {
            throw new InvalidOperationException($"La dieta ya tiene la restricción {restriccionAlimentariaId}.");
        }

        var restriccion = new DietaRestriccion(Id, restriccionAlimentariaId);
        _restricciones.Add(restriccion);
        return restriccion;
    }
}
