using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.Tests.Salas;

/// <summary>Catálogo de salas en memoria para probar los manejadores del #72 sin EF Core (ADR-010, §9).</summary>
internal sealed class BancoDeSalasFalso
{
    public List<Sala> Salas { get; } = [];

    public List<Paciente> Pacientes { get; } = [];

    public UnidadDeTrabajoFalsaDeSalas UnidadDeTrabajo { get; } = new();

    public SalasFalsas SalasRepo => new(this);

    public PacientesFalsosDeSalas PacientesRepo => new(this);

    public Sala Agregar(string nombre)
    {
        var sala = new Sala(Guid.NewGuid(), nombre);
        Salas.Add(sala);
        return sala;
    }
}

internal sealed class SalasFalsas(BancoDeSalasFalso banco) : ISalaRepository
{
    public Task<Sala?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Salas.Find(s => s.Id == id));

    public Task<IReadOnlyList<Sala>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Sala>>([.. banco.Salas]);

    public Task AddAsync(Sala entity, CancellationToken cancellationToken = default)
    {
        banco.Salas.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Sala entity) => banco.Salas.Remove(entity);

    public Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId = null, CancellationToken cancellationToken = default)
    {
        var normalizado = nombre.Trim().ToLowerInvariant();
        return Task.FromResult(banco.Salas.Any(s =>
            (excluirId is null || s.Id != excluirId) && s.Nombre.Trim().ToLowerInvariant() == normalizado));
    }
}

/// <summary>Solo lo que necesita <see cref="Menus.Application.Salas.EliminarSala.EliminarSalaHandler"/>.</summary>
internal sealed class PacientesFalsosDeSalas(BancoDeSalasFalso banco) : IPacienteRepository
{
    public Task<Paciente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<Paciente>> ListAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task AddAsync(Paciente entity, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public void Remove(Paciente entity) => throw new NotSupportedException();

    public Task<IReadOnlyList<Paciente>> GetBySalaAsync(Guid salaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Paciente>>([.. banco.Pacientes.Where(p => p.SalaId == salaId)]);

    public Task<IReadOnlyList<Paciente>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<(IReadOnlyList<Paciente> Pacientes, int Total)> BuscarAsync(
        Guid? salaId, Guid? dietaId, int pagina, int tamano, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

internal sealed class UnidadDeTrabajoFalsaDeSalas : IUnitOfWork
{
    public int Confirmaciones { get; private set; }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        Confirmaciones++;
        return Task.FromResult(1);
    }
}
