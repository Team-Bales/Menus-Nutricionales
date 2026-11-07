using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="ProgramaAtencion"/> sobre EF Core.</summary>
public sealed class ProgramaAtencionRepository : Repository<ProgramaAtencion, Guid>, INomencladorRepository<ProgramaAtencion>
{
    /// <param name="context">Contexto de persistencia.</param>
    public ProgramaAtencionRepository(MenusDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => await Context.ProgramasAtencion.AnyAsync(p => p.Nombre == nombre, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>Lo referencia <c>Dieta.ProgramaAtencionId</c> (RESTRICT, RR-12).</remarks>
    public async Task<bool> EstaEnUsoAsync(Guid id, CancellationToken cancellationToken = default)
        => await Context.Dietas.AnyAsync(d => d.ProgramaAtencionId == id, cancellationToken);
}
