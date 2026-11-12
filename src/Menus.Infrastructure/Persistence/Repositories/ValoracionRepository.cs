using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="ValoracionNutricional"/> sobre EF Core.</summary>
public sealed class ValoracionRepository : Repository<ValoracionNutricional, Guid>, IValoracionRepository
{
    /// <param name="context">Contexto de persistencia.</param>
    public ValoracionRepository(MenusDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ValoracionNutricional>> GetByPacienteAsync(
        Guid pacienteId,
        CancellationToken cancellationToken = default)
        => await Context.ValoracionesNutricionales
            .AsNoTracking()
            .Where(v => v.PacienteId == pacienteId)
            .OrderByDescending(v => v.FechaValoracion)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ValoracionNutricional>> GetByDietaAsync(
        Guid dietaId,
        CancellationToken cancellationToken = default)
        => await (
            from v in Context.ValoracionesNutricionales.AsNoTracking()
            join m in Context.Menus.AsNoTracking() on v.MenuId equals m.Id
            where m.DietaId == dietaId
            orderby v.FechaValoracion descending
            select v)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ValoracionNutricional>> GetByPacienteYDietaAsync(
        Guid pacienteId,
        Guid dietaId,
        CancellationToken cancellationToken = default)
        => await (
            from v in Context.ValoracionesNutricionales.AsNoTracking()
            join m in Context.Menus.AsNoTracking() on v.MenuId equals m.Id
            where v.PacienteId == pacienteId && m.DietaId == dietaId
            orderby v.FechaValoracion descending
            select v)
            .ToListAsync(cancellationToken);
}
