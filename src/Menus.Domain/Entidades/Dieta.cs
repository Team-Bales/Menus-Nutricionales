using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

public sealed class Dieta : Entity<Guid>
{
    private readonly List<DietaGrupoNutricional> _gruposNutricionales = [];
    private readonly List<DietaRestriccion> _restricciones = [];

    public Dieta(Guid id, string nombre, Guid programaAtencionId) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
        ProgramaAtencionId = programaAtencionId;
    }

    public string Nombre { get; }
    public Guid ProgramaAtencionId { get; }

    public IReadOnlyCollection<DietaGrupoNutricional> GruposNutricionales => _gruposNutricionales.AsReadOnly();
    public IReadOnlyCollection<DietaRestriccion> Restricciones => _restricciones.AsReadOnly();

    public DietaGrupoNutricional AgregarGrupo(Guid grupoNutricionalId)
    {
        if (_gruposNutricionales.Any(g => g.GrupoNutricionalId == grupoNutricionalId))
        {
            throw new InvalidOperationException($"La dieta ya cubre el grupo nutricional {grupoNutricionalId}.");
        }

        var grupo = new DietaGrupoNutricional(Id, grupoNutricionalId);
        _gruposNutricionales.Add(grupo);
        return grupo;
    }

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
