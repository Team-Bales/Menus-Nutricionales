using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="Dieta"/> sobre EF Core.</summary>
public sealed class DietaRepository : Repository<Dieta, Guid>, IDietaRepository
{
    /// <param name="context">Contexto de persistencia.</param>
    public DietaRepository(MenusDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<Dieta?> GetConGruposYRestriccionesAsync(Guid id, CancellationToken cancellationToken = default)
        => await Context.Dietas
            .Include(d => d.GruposNutricionales)
            .Include(d => d.Restricciones)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Dieta>> GetByProgramaAtencionAsync(
        Guid programaAtencionId,
        CancellationToken cancellationToken = default)
        => await Context.Dietas
            .AsNoTracking()
            .Where(d => d.ProgramaAtencionId == programaAtencionId)
            .ToListAsync(cancellationToken);
}
