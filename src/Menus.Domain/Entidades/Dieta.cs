using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

public sealed class Dieta : Entity<Guid>
{
    public string Nombre { get; private set; }
    public Guid ProgramaAtencionId { get; private set; }

    private readonly List<DietaGrupoNutricional> _gruposNutricionales = new();
    private readonly List<DietaRestriccion> _restricciones = new();

    public IReadOnlyCollection<DietaGrupoNutricional> GruposNutricionales => _gruposNutricionales.AsReadOnly();
    public IReadOnlyCollection<DietaRestriccion> Restricciones => _restricciones.AsReadOnly();

    public Dieta(Guid id, string nombre, Guid programaAtencionId) : base(id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El Id no puede ser vacío.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre no puede estar vacío.", nameof(nombre));
        }

        if (programaAtencionId == Guid.Empty)
        {
            throw new ArgumentException("El Id del programa de atención no puede ser vacío.", nameof(programaAtencionId));
        }

        Nombre = nombre;
        ProgramaAtencionId = programaAtencionId;
    }

    public void AgregarGrupo(Guid grupoNutricionalId)
    {
        if (grupoNutricionalId == Guid.Empty)
        {
            throw new ArgumentException("El Id del grupo nutricional es inválido.", nameof(grupoNutricionalId));
        }

        if (_gruposNutricionales.Any(g => g.GrupoNutricionalId == grupoNutricionalId))
        {
            return;
        }

        _gruposNutricionales.Add(new DietaGrupoNutricional(Id, grupoNutricionalId));
    }

    public void AgregarRestriccion(Guid restriccionAlimentariaId)
    {
        if (restriccionAlimentariaId == Guid.Empty)
        {
            throw new ArgumentException("El Id de la restricción es inválido.", nameof(restriccionAlimentariaId));
        }

        if (_restricciones.Any(r => r.RestriccionAlimentariaId == restriccionAlimentariaId))
        {
            return;
        }

        _restricciones.Add(new DietaRestriccion(Id, restriccionAlimentariaId));
    }
}
