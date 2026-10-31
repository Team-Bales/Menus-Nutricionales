using Menus.Domain.Entidades;
using Menus.Infrastructure.Persistence;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="Sala"/> sobre EF Core, con las operaciones básicas del repositorio genérico.</summary>
public sealed class SalaRepository : Repository<Sala, Guid>
{
    /// <param name="context">Contexto de persistencia.</param>
    public SalaRepository(MenusDbContext context) : base(context)
    {
    }
}
