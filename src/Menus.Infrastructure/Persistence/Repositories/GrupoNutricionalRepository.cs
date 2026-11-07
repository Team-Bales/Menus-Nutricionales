using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="GrupoNutricional"/> sobre EF Core.</summary>
public sealed class GrupoNutricionalRepository : Repository<GrupoNutricional, Guid>, INomencladorRepository<GrupoNutricional>
{
    /// <param name="context">Contexto de persistencia.</param>
    public GrupoNutricionalRepository(MenusDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => await Context.GruposNutricionales.AnyAsync(g => g.Nombre == nombre, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// Lo referencian <see cref="ItemAlimentario"/> (<c>Pertenece</c>), <see cref="DietaGrupoNutricional"/>
    /// (<c>CubreDieta</c>), <see cref="RestriccionGrupoExcluido"/> (<c>Excluye</c>) y
    /// <see cref="MenuGrupoNutricional"/> (<c>CubreMenú</c>), todas en <c>RESTRICT</c>.
    /// </remarks>
    public async Task<bool> EstaEnUsoAsync(Guid id, CancellationToken cancellationToken = default)
        => await Context.ItemsAlimentarios.AnyAsync(i => i.GrupoNutricionalId == id, cancellationToken)
        || await Context.DietasGruposNutricionales.AnyAsync(dg => dg.GrupoNutricionalId == id, cancellationToken)
        || await Context.RestriccionesGruposExcluidos.AnyAsync(e => e.GrupoNutricionalId == id, cancellationToken)
        || await Context.MenuGruposNutricionales.AnyAsync(mg => mg.GrupoNutricionalId == id, cancellationToken);
}
