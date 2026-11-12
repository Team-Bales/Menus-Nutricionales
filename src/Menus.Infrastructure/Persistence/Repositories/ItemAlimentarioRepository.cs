using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="ItemAlimentario"/> (alimentos y platos a la vez) sobre EF Core.</summary>
public sealed class ItemAlimentarioRepository : Repository<ItemAlimentario, Guid>, IItemAlimentarioRepository
{
    /// <param name="context">Contexto de persistencia.</param>
    public ItemAlimentarioRepository(MenusDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Compara <c>lower(btrim(nombre))</c>, la misma expresión del índice único de RC-01
    /// (<c>Persistence/Sql/R5_16_nombre_normalizado.sql</c>): la consulta y la base dicen lo mismo, y la consulta usa
    /// el índice. Por eso la columna va con <see cref="string.ToLower()"/>, que Npgsql traduce a <c>lower</c>;
    /// <see cref="string.ToLowerInvariant"/> no tiene traducción.
    /// </remarks>
    public Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nombre);
        var normalizado = nombre.Trim().ToLowerInvariant();

        return Context.ItemsAlimentarios.AnyAsync(i => i.Nombre.Trim().ToLower() == normalizado, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> FiguraEnAlgunMenuAsync(Guid itemId, CancellationToken cancellationToken = default)
        => Context.MenuItems.AnyAsync(mi => mi.ItemId == itemId, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<Guid, int>> ContarPorNivelCaloricoAsync(
        IReadOnlyCollection<Guid> gruposProhibidos,
        CancellationToken cancellationToken = default)
    {
        var conteos = await Context.ItemsAlimentarios
            .AsNoTracking()
            .Where(i => !gruposProhibidos.Contains(i.GrupoNutricionalId))
            .GroupBy(i => i.NivelCaloricoId)
            .Select(g => new { NivelCaloricoId = g.Key, Total = g.Count() })
            .ToListAsync(cancellationToken);

        return conteos.ToDictionary(c => c.NivelCaloricoId, c => c.Total);
    }
}
