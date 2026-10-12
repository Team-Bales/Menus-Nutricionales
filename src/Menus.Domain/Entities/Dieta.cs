namespace Menus.Domain.Entities;

public class Dieta
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; }
    public Guid ProgramaAtencionId { get; private set; }

    // Colecciones encapsuladas (backing fields)
    private readonly List<DietaGrupoNutricional> _gruposNutricionales = new();
    private readonly List<DietaRestriccion> _restricciones = new();

    // Colecciones de sólo lectura expuestas hacia el exterior
    public IReadOnlyCollection<DietaGrupoNutricional> GruposNutricionales => _gruposNutricionales.AsReadOnly();
    public IReadOnlyCollection<DietaRestriccion> Restricciones => _restricciones.AsReadOnly();

    private Dieta() { } // Para ORM

    public Dieta(Guid id, string nombre, Guid programaAtencionId)
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

        Id = id;
        Nombre = nombre;
        ProgramaAtencionId = programaAtencionId;
    }

    public void AgregarGrupo(Guid grupoNutricionalId)
    {
        if (grupoNutricionalId == Guid.Empty)
        {
            throw new ArgumentException("El Id del grupo nutricional es inválido.", nameof(grupoNutricionalId));
        }

        // Evita duplicados: si ya existe, no hace nada
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

        // Evita duplicados: si ya existe, no lanza error ni agrega
        if (_restricciones.Any(r => r.RestriccionAlimentariaId == restriccionAlimentariaId))
        {
            return;
        }

        _restricciones.Add(new DietaRestriccion(Id, restriccionAlimentariaId));
    }
}
