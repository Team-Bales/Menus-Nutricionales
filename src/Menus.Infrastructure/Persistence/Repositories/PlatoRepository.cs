using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="Plato"/> sobre EF Core.</summary>
public sealed class PlatoRepository : Repository<Plato, Guid>, IPlatoRepository
{
    /// <param name="context">Contexto de persistencia.</param>
    public PlatoRepository(MenusDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<Plato?> GetConIngredientesAsync(Guid id, CancellationToken cancellationToken = default)
        => await Context.Platos
            .Include(p => p.Ingredientes)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
}
