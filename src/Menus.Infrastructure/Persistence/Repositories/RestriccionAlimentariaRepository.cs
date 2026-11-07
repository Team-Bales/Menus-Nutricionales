using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="RestriccionAlimentaria"/> sobre EF Core.</summary>
public sealed class RestriccionAlimentariaRepository
    : Repository<RestriccionAlimentaria, Guid>, INomencladorRepository<RestriccionAlimentaria>
{
    /// <param name="context">Contexto de persistencia.</param>
    public RestriccionAlimentariaRepository(MenusDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => await Context.RestriccionesAlimentarias.AnyAsync(r => r.Nombre == nombre, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// La referencian <see cref="DietaRestriccion"/> (<c>Restringe</c>, RESTRICT) y sus propios
    /// <see cref="RestriccionAlimentaria.GruposExcluidos"/> (<c>Excluye</c>, también RESTRICT: no se elimina una
    /// restricción con exclusiones sin quitarlas antes).
    /// </remarks>
    public async Task<bool> EstaEnUsoAsync(Guid id, CancellationToken cancellationToken = default)
        => await Context.DietasRestricciones.AnyAsync(dr => dr.RestriccionAlimentariaId == id, cancellationToken)
        || await Context.RestriccionesGruposExcluidos.AnyAsync(e => e.RestriccionAlimentariaId == id, cancellationToken);
}
