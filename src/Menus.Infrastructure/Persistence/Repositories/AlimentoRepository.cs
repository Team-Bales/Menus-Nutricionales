using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="Alimento"/> sobre EF Core.</summary>
public sealed class AlimentoRepository : Repository<Alimento, Guid>, IAlimentoRepository
{
    /// <param name="context">Contexto de persistencia.</param>
    public AlimentoRepository(MenusDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Alimento>> GetByGrupoNutricionalAsync(
        Guid grupoNutricionalId,
        CancellationToken cancellationToken = default)
        => await Context.Alimentos
            .AsNoTracking()
            .Where(a => a.GrupoNutricionalId == grupoNutricionalId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Alimento>> GetByNivelCaloricoAsync(
        NivelCalorico nivelCalorico,
        CancellationToken cancellationToken = default)
        => await Context.Alimentos
            .AsNoTracking()
            .Where(a => a.NivelCaloricoId == nivelCalorico.Id)
            .ToListAsync(cancellationToken);
}
