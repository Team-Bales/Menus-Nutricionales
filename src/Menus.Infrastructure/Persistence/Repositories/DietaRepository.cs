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

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Dieta> Dietas, int Total)> BuscarAsync(
        Guid? programaAtencionId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        var consulta = Context.Dietas.AsNoTracking();

        if (programaAtencionId is { } programa)
        {
            consulta = consulta.Where(d => d.ProgramaAtencionId == programa);
        }

        var total = await consulta.CountAsync(cancellationToken);

        // El Id desempata los nombres iguales, para que la paginación sea estable.
        var dietas = await consulta
            .OrderBy(d => d.Nombre)
            .ThenBy(d => d.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancellationToken);

        return (dietas, total);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Dieta>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
        => await Context.Dietas
            .AsNoTracking()
            .Where(d => ids.Contains(d.Id))
            .ToListAsync(cancellationToken);
}
