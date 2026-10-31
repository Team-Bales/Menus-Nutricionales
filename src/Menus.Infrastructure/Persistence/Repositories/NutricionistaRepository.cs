using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="Nutricionista"/> sobre EF Core.</summary>
public sealed class NutricionistaRepository : Repository<Nutricionista, Guid>, INutricionistaRepository
{
    /// <param name="context">Contexto de persistencia.</param>
    public NutricionistaRepository(MenusDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public override Task<Nutricionista?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Context.Nutricionistas
            .Include(n => n.DietasAutorizadas)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Nutricionista>> GetByDietaAsync(
        Guid dietaId,
        CancellationToken cancellationToken = default)
        => await Context.Nutricionistas
            .AsNoTracking()
            .Where(n => n.DietasAutorizadas.Any(a => a.DietaId == dietaId))
            .ToListAsync(cancellationToken);
}
