using Menus.Domain.Abstractions;
using Menus.Domain.Entidades;

namespace Menus.Application.Tests.Pacientes;

/// <summary>
/// Salas en memoria, para los manejadores del #73. Los pacientes y las dietas los pone
/// <see cref="Catalogo.Dietas.BancoDeDietasFalso"/>.
/// </summary>
internal sealed class SalasFalsas : IRepository<Sala, Guid>
{
    public List<Sala> Salas { get; } = [];

    public Sala Agregar(string nombre)
    {
        var sala = new Sala(Guid.NewGuid(), nombre);
        Salas.Add(sala);
        return sala;
    }

    public Task<Sala?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Salas.Find(s => s.Id == id));

    public Task<IReadOnlyList<Sala>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Sala>>([.. Salas]);

    public Task AddAsync(Sala entity, CancellationToken cancellationToken = default)
    {
        Salas.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Sala entity) => Salas.Remove(entity);
}
