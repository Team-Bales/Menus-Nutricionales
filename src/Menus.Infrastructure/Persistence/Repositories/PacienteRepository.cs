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
}
