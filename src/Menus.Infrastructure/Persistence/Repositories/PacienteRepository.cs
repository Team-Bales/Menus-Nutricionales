using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="Paciente"/> sobre EF Core.</summary>
public sealed class PacienteRepository : Repository<Paciente, Guid>, IPacienteRepository
{
    /// <param name="context">Contexto de persistencia.</param>
    public PacienteRepository(MenusDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public override Task<Paciente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Context.Pacientes
            .Include(p => p.Dietas)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Paciente>> GetBySalaAsync(
        Guid salaId,
        CancellationToken cancellationToken = default)
        => await Context.Pacientes
            .AsNoTracking()
            .Where(p => p.SalaId == salaId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Paciente>> GetByDietaAsync(
        Guid dietaId,
        CancellationToken cancellationToken = default)
        => await Context.Pacientes
            .AsNoTracking()
            .Where(p => p.Dietas.Any(d => d.DietaId == dietaId))
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Paciente> Pacientes, int Total)> BuscarAsync(
        Guid? salaId,
        Guid? dietaId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        var consulta = Context.Pacientes.AsNoTracking();

        if (salaId is { } sala)
        {
            consulta = consulta.Where(p => p.SalaId == sala);
        }

        if (dietaId is { } dieta)
        {
            consulta = consulta.Where(p => p.Dietas.Any(d => d.DietaId == dieta));
        }

        var total = await consulta.CountAsync(cancellationToken);

        // El Id desempata los nombres iguales, para que la paginación sea estable.
        var pacientes = await consulta
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancellationToken);

        return (pacientes, total);
    }
}
