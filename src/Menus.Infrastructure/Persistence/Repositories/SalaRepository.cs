using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="Sala"/> sobre EF Core.</summary>
public sealed class SalaRepository : Repository<Sala, Guid>, ISalaRepository
{
    /// <param name="context">Contexto de persistencia.</param>
    public SalaRepository(MenusDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId = null, CancellationToken cancellationToken = default)
    {
        var normalizado = nombre.Trim().ToLowerInvariant();
        return Context.Salas
            .Where(s => excluirId == null || s.Id != excluirId)
            .AnyAsync(s => s.Nombre.Trim().ToLower() == normalizado, cancellationToken);
    }
}
