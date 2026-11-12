using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Revalorizaciones;

/// <summary>
/// Banco en memoria para probar los manejadores de revalorizaciones sin EF Core (ADR-010, §9).
/// </summary>
internal sealed class RevalorizacionesFalso
{
    public static readonly Guid NutricionistaEjecutor = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    public static readonly Guid NutricionistaOtro = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    public static readonly Guid Paciente = Guid.Parse("dddddddd-0000-0000-0000-000000000001");
    public static readonly Guid Menu = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");
    public static readonly Guid ValoracionRevisada = Guid.Parse("ffffffff-0000-0000-0000-000000000001");

    public List<Revalorizacion> Revalorizaciones { get; } = [];
    public List<ValoracionNutricional> Valoraciones { get; } = [];

    public UnidadDeTrabajoFalsa UnidadDeTrabajo { get; } = new();

    public RevalorizacionesFalsoRepo RevalorizacionesRepo => new(this);
    public ValoracionesFalsoRepo ValoracionesRepo => new(this);
    public NutricionistasFalsoRepo NutricionistasRepo => new(this);

    public void Sembrar()
    {
        // Valoración revisada existente
        Valoraciones.Add(new ValoracionNutricional(
            ValoracionRevisada,
            Menu,
            Paciente,
            NutricionistaEjecutor,
            ResultadoNutricional.Normal.Id,
            DateTime.UtcNow.AddDays(-1)));
    }
}

internal sealed class RevalorizacionesFalsoRepo(RevalorizacionesFalso banco) : IRevalorizacionRepository
{
    public Task<Revalorizacion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Revalorizaciones.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<Revalorizacion>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Revalorizacion>>([.. banco.Revalorizaciones]);

    public Task AddAsync(Revalorizacion entity, CancellationToken cancellationToken = default)
    {
        banco.Revalorizaciones.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Revalorizacion entity) => banco.Revalorizaciones.Remove(entity);

    public Task<IReadOnlyList<Revalorizacion>> GetPendientesByNutricionistaAsync(Guid nutricionistaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Revalorizacion>>(
            [.. banco.Revalorizaciones
                .Where(r => r.NutricionistaId == nutricionistaId && r.ValoracionGeneradaId == null)
                .OrderBy(r => r.FechaSolicitud)]);

    public Task<IReadOnlyList<Revalorizacion>> GetByPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Revalorizacion>>(
            [.. banco.Revalorizaciones
                .Join(banco.Valoraciones,
                    r => r.ValoracionRevisadaId,
                    v => v.Id,
                    (r, v) => new { Revalorizacion = r, Valoracion = v })
                .Where(x => x.Valoracion.PacienteId == pacienteId)
                .Select(x => x.Revalorizacion)
                .OrderBy(r => r.FechaSolicitud)]);
}

internal sealed class ValoracionesFalsoRepo(RevalorizacionesFalso banco) : IValoracionRepository
{
    public Task<ValoracionNutricional?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Valoraciones.FirstOrDefault(v => v.Id == id));

    public Task<IReadOnlyList<ValoracionNutricional>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ValoracionNutricional>>([.. banco.Valoraciones]);

    public Task AddAsync(ValoracionNutricional entity, CancellationToken cancellationToken = default)
    {
        banco.Valoraciones.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(ValoracionNutricional entity) => banco.Valoraciones.Remove(entity);

    public Task<IReadOnlyList<ValoracionNutricional>> GetByPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ValoracionNutricional>>(
            [.. banco.Valoraciones.Where(v => v.PacienteId == pacienteId).OrderByDescending(v => v.FechaValoracion)]);

    public Task<IReadOnlyList<ValoracionNutricional>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ValoracionNutricional>>([]);

    public Task<IReadOnlyList<ValoracionNutricional>> GetByPacienteYDietaAsync(Guid pacienteId, Guid dietaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ValoracionNutricional>>([]);
}

internal sealed class NutricionistasFalsoRepo(RevalorizacionesFalso banco) : INutricionistaRepository
{
    public static readonly Guid Especialidad = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    public Task<Nutricionista?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == RevalorizacionesFalso.NutricionistaEjecutor || id == RevalorizacionesFalso.NutricionistaOtro)
        {
            return Task.FromResult<Nutricionista?>(new Nutricionista(id, "Nutricionista", Especialidad));
        }
        return Task.FromResult<Nutricionista?>(null);
    }

    public Task<IReadOnlyList<Nutricionista>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Nutricionista>>([]);

    public Task AddAsync(Nutricionista entity, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public void Remove(Nutricionista entity) { }

    public Task<IReadOnlyList<Nutricionista>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Nutricionista>>([]);
}

internal sealed class UnidadDeTrabajoFalsa : IUnitOfWork
{
    public int Confirmaciones { get; private set; }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        Confirmaciones++;
        return Task.FromResult(1);
    }
}

internal sealed class ActorFalso(Guid id) : IActorActual
{
    public Guid Id { get; } = id;
}

internal sealed class CacheFalso : ICache
{
    public Task<T?> ObtenerAsync<T>(string clave, CancellationToken ct = default) where T : class
        => Task.FromResult<T?>(null);

    public Task GuardarAsync<T>(string clave, T valor, TimeSpan tiempoDeVida, CancellationToken ct = default) where T : class
        => Task.CompletedTask;

    public Task InvalidarAsync(string clave, CancellationToken ct = default)
        => Task.CompletedTask;
}