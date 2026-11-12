using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="Revalorizacion"/> sobre EF Core.</summary>
public sealed class RevalorizacionRepository : Repository<Revalorizacion, Guid>, IRevalorizacionRepository
{
    /// <param name="context">Contexto de persistencia.</param>
    public RevalorizacionRepository(MenusDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Revalorizacion>> GetPendientesByNutricionistaAsync(
        Guid nutricionistaId,
        CancellationToken cancellationToken = default)
        => await Set
            .AsNoTracking()
            .Where(r => r.NutricionistaId == nutricionistaId && r.ValoracionGeneradaId == null)
            .OrderBy(r => r.FechaSolicitud)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Revalorizacion>> GetByPacienteAsync(
        Guid pacienteId,
        CancellationToken cancellationToken = default)
        => await Set
            .AsNoTracking()
            .Join(
                Context.ValoracionesNutricionales,
                r => r.ValoracionRevisadaId,
                v => v.Id,
                (r, v) => new { Revalorizacion = r, Valoracion = v })
            .Where(x => x.Valoracion.PacienteId == pacienteId)
            .Select(x => x.Revalorizacion)
            .ToListAsync(cancellationToken);
}