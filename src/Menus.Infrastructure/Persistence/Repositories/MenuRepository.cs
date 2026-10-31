using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación EF Core de <see cref="IMenuRepository"/>.
/// </summary>
internal sealed class MenuRepository(MenusDbContext dbContext) : IMenuRepository
{
    public async Task<Menu?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Menus
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Menu>> ListAsync(CancellationToken cancellationToken = default)
        => await dbContext.Menus
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Menu entity, CancellationToken cancellationToken = default)
        => await dbContext.Menus.AddAsync(entity, cancellationToken);

    public void Remove(Menu entity)
        => dbContext.Menus.Remove(entity);

    public async Task<IReadOnlyList<Menu>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => await dbContext.Menus
            .AsNoTracking()
            .Where(m => m.DietaId == dietaId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Menu>> GetByNutricionistaCreadorAsync(Guid nutricionistaId, CancellationToken cancellationToken = default)
        => await dbContext.Menus
            .AsNoTracking()
            .Where(m => m.NutricionistaCreadorId == nutricionistaId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Menu>> GetByPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default)
        => await dbContext.Menus
            .AsNoTracking()
            .Where(m => m.Asignaciones.Any(a => a.PacienteId == pacienteId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Menu>> GetFinalesByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => await dbContext.Menus
            .AsNoTracking()
            .Where(m => m.DietaId == dietaId && m.EsFinal)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Menu>> GetPendientesByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => await dbContext.Menus
            .AsNoTracking()
            .Where(m => m.DietaId == dietaId && m.EsPendiente)
            .ToListAsync(cancellationToken);
}
