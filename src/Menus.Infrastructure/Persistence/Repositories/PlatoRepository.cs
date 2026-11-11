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

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Plato> Platos, int Total)> BuscarAsync(
        Guid? grupoNutricionalId,
        Guid? tipoPreparacionId,
        Guid? nivelCaloricoId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        var consulta = Context.Platos.AsNoTracking();

        if (grupoNutricionalId is { } grupo)
        {
            consulta = consulta.Where(p => p.GrupoNutricionalId == grupo);
        }

        if (tipoPreparacionId is { } tipo)
        {
            consulta = consulta.Where(p => p.TipoPreparacionId == tipo);
        }

        if (nivelCaloricoId is { } nivel)
        {
            consulta = consulta.Where(p => p.NivelCaloricoId == nivel);
        }

        var total = await consulta.CountAsync(cancellationToken);

        // El Id desempata los nombres iguales salvo mayúsculas, para que la paginación sea estable.
        var platos = await consulta
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancellationToken);

        return (platos, total);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<Guid, int>> ContarIngredientesAsync(
        IReadOnlyCollection<Guid> platoIds,
        CancellationToken cancellationToken = default)
        => await Context.PlatosIngredientes
            .Where(i => platoIds.Contains(i.PlatoId))
            .GroupBy(i => i.PlatoId)
            .Select(g => new { PlatoId = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(g => g.PlatoId, g => g.Cantidad, cancellationToken);
}
