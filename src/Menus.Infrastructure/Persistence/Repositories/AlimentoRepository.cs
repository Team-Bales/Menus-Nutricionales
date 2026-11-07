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

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Alimento> Alimentos, int Total)> BuscarAsync(
        Guid? grupoNutricionalId,
        Guid? tipoPreparacionId,
        Guid? nivelCaloricoId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        var consulta = Context.Alimentos.AsNoTracking();

        if (grupoNutricionalId is { } grupo)
        {
            consulta = consulta.Where(a => a.GrupoNutricionalId == grupo);
        }

        if (tipoPreparacionId is { } tipo)
        {
            consulta = consulta.Where(a => a.TipoPreparacionId == tipo);
        }

        if (nivelCaloricoId is { } nivel)
        {
            consulta = consulta.Where(a => a.NivelCaloricoId == nivel);
        }

        var total = await consulta.CountAsync(cancellationToken);

        // El Id desempata los nombres iguales salvo mayúsculas, para que la paginación sea estable.
        var alimentos = await consulta
            .OrderBy(a => a.Nombre)
            .ThenBy(a => a.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancellationToken);

        return (alimentos, total);
    }

    /// <inheritdoc/>
    public Task<bool> FormaParteDeAlgunPlatoAsync(Guid alimentoId, CancellationToken cancellationToken = default)
        => Context.PlatosIngredientes.AnyAsync(i => i.AlimentoId == alimentoId, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Alimento>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
        => await Context.Alimentos
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToListAsync(cancellationToken);
}
