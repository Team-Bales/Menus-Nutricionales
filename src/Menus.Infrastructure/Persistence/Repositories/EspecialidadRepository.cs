using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de <see cref="Especialidad"/> sobre EF Core.</summary>
public sealed class EspecialidadRepository : Repository<Especialidad, Guid>, INomencladorRepository<Especialidad>
{
    /// <param name="context">Contexto de persistencia.</param>
    public EspecialidadRepository(MenusDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => await Context.Especialidades.AnyAsync(e => e.Nombre == nombre, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>La referencia <c>Nutricionista.EspecialidadId</c> (RESTRICT, RR-12).</remarks>
    public async Task<bool> EstaEnUsoAsync(Guid id, CancellationToken cancellationToken = default)
        => await Context.Nutricionistas.AnyAsync(n => n.EspecialidadId == id, cancellationToken);
}
