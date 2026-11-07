using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Catalogo.Nomencladores;

/// <summary>
/// Catálogo de nomencladores en memoria para probar los manejadores del #64 sin EF Core (ADR-010, §9), igual que
/// <see cref="Alimentos.BancoFalso"/>.
/// </summary>
internal sealed class BancoDeNomencladoresFalso
{
    public List<GrupoNutricional> Grupos { get; } = [];

    public List<RestriccionAlimentaria> Restricciones { get; } = [];

    public List<ProgramaAtencion> Programas { get; } = [];

    public List<Especialidad> Especialidades { get; } = [];

    /// <summary>Identificadores de cada catálogo que el repositorio falso considera «en uso».</summary>
    public HashSet<Guid> EnUso { get; } = [];

    public UnidadDeTrabajoFalsaDeNomencladores UnidadDeTrabajo { get; } = new();

    public GruposNutricionalesFalsos GruposRepo => new(this);

    public RestriccionesAlimentariasFalsas RestriccionesRepo => new(this);

    public ProgramasAtencionFalsos ProgramasRepo => new(this);

    public EspecialidadesFalsas EspecialidadesRepo => new(this);

    public GrupoNutricional AgregarGrupo(string nombre)
    {
        var grupo = new GrupoNutricional(Guid.NewGuid(), nombre);
        Grupos.Add(grupo);
        return grupo;
    }

    public RestriccionAlimentaria AgregarRestriccion(string nombre)
    {
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), nombre);
        Restricciones.Add(restriccion);
        return restriccion;
    }

    public ProgramaAtencion AgregarPrograma(string nombre)
    {
        var programa = new ProgramaAtencion(Guid.NewGuid(), nombre);
        Programas.Add(programa);
        return programa;
    }

    public Especialidad AgregarEspecialidad(string nombre)
    {
        var especialidad = new Especialidad(Guid.NewGuid(), nombre);
        Especialidades.Add(especialidad);
        return especialidad;
    }
}

internal sealed class GruposNutricionalesFalsos(BancoDeNomencladoresFalso banco) : INomencladorRepository<GrupoNutricional>
{
    public Task<GrupoNutricional?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Grupos.Find(g => g.Id == id));

    public Task<IReadOnlyList<GrupoNutricional>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GrupoNutricional>>([.. banco.Grupos]);

    public Task AddAsync(GrupoNutricional entity, CancellationToken cancellationToken = default)
    {
        banco.Grupos.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(GrupoNutricional entity) => banco.Grupos.Remove(entity);

    public Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Grupos.Exists(g => g.Nombre == nombre));

    public Task<bool> EstaEnUsoAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.EnUso.Contains(id));
}

internal sealed class RestriccionesAlimentariasFalsas(BancoDeNomencladoresFalso banco) : INomencladorRepository<RestriccionAlimentaria>
{
    public Task<RestriccionAlimentaria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Restricciones.Find(r => r.Id == id));

    public Task<IReadOnlyList<RestriccionAlimentaria>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RestriccionAlimentaria>>([.. banco.Restricciones]);

    public Task AddAsync(RestriccionAlimentaria entity, CancellationToken cancellationToken = default)
    {
        banco.Restricciones.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(RestriccionAlimentaria entity) => banco.Restricciones.Remove(entity);

    public Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Restricciones.Exists(r => r.Nombre == nombre));

    public Task<bool> EstaEnUsoAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.EnUso.Contains(id));
}

internal sealed class ProgramasAtencionFalsos(BancoDeNomencladoresFalso banco) : INomencladorRepository<ProgramaAtencion>
{
    public Task<ProgramaAtencion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Programas.Find(p => p.Id == id));

    public Task<IReadOnlyList<ProgramaAtencion>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ProgramaAtencion>>([.. banco.Programas]);

    public Task AddAsync(ProgramaAtencion entity, CancellationToken cancellationToken = default)
    {
        banco.Programas.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(ProgramaAtencion entity) => banco.Programas.Remove(entity);

    public Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Programas.Exists(p => p.Nombre == nombre));

    public Task<bool> EstaEnUsoAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.EnUso.Contains(id));
}

internal sealed class EspecialidadesFalsas(BancoDeNomencladoresFalso banco) : INomencladorRepository<Especialidad>
{
    public Task<Especialidad?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Especialidades.Find(e => e.Id == id));

    public Task<IReadOnlyList<Especialidad>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Especialidad>>([.. banco.Especialidades]);

    public Task AddAsync(Especialidad entity, CancellationToken cancellationToken = default)
    {
        banco.Especialidades.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Especialidad entity) => banco.Especialidades.Remove(entity);

    public Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Especialidades.Exists(e => e.Nombre == nombre));

    public Task<bool> EstaEnUsoAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.EnUso.Contains(id));
}

internal sealed class UnidadDeTrabajoFalsaDeNomencladores : IUnitOfWork
{
    public int Confirmaciones { get; private set; }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        Confirmaciones++;
        return Task.FromResult(1);
    }
}
