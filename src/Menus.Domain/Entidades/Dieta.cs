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

    /// <summary>Quita un grupo nutricional de la cobertura de la dieta, sea requerido o restringido.</summary>
    /// <remarks>
    /// No se quita el último grupo requerido: toda dieta exige cubrir al menos uno (RK-07), y la base lo
    /// comprueba igual al confirmar. Los menús ya creados no cambian: su cobertura es su propia parametrización
    /// (RS-07). El caso de uso invalida la caché de disponibilidad calórica de la dieta (informe, «Consultas
    /// recurrentes»).
    /// </remarks>
    /// <param name="grupoNutricionalId">Identificador del grupo nutricional que se quita.</param>
    /// <exception cref="InvalidOperationException">
    /// Si la dieta no cubre ese grupo, o si es su único grupo requerido (RK-07).
    /// </exception>
    public void QuitarGrupo(Guid grupoNutricionalId)
    {
        var grupo = _gruposNutricionales.SingleOrDefault(g => g.GrupoNutricionalId == grupoNutricionalId)
            ?? throw new InvalidOperationException($"La dieta no cubre el grupo nutricional {grupoNutricionalId}.");

        if (grupo.Tipo == TipoCobertura.Requerido
            && _gruposNutricionales.Count(g => g.Tipo == TipoCobertura.Requerido) == 1)
        {
            throw new InvalidOperationException("Toda dieta exige cubrir al menos un grupo nutricional (RK-07): no se quita su único grupo requerido.");
        }

        _gruposNutricionales.Remove(grupo);
    }

    /// <summary>Quita una restricción alimentaria de la dieta.</summary>
    /// <remarks>
    /// Una dieta puede no declarar restricciones (informe §4.5). El caso de uso invalida la caché de
    /// disponibilidad calórica de la dieta, porque cambian sus grupos prohibidos.
    /// </remarks>
    /// <param name="restriccionAlimentariaId">Identificador de la restricción alimentaria que se quita.</param>
    /// <exception cref="InvalidOperationException">Si la dieta no tiene esa restricción.</exception>
    public void QuitarRestriccion(Guid restriccionAlimentariaId)
    {
        var restriccion = _restricciones.SingleOrDefault(r => r.RestriccionAlimentariaId == restriccionAlimentariaId)
            ?? throw new InvalidOperationException($"La dieta no tiene la restricción {restriccionAlimentariaId}.");

        _restricciones.Remove(restriccion);
    }
}
